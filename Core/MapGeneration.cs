using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Civ2Like.Core;

internal static class MapGeneration
{
    public enum WorldFlavor
    {
        Continental,
        Islands
    }

    public static class ImproveGame
    {
        /// <summary>
        /// Generates a wrapped (toroidal) map using periodic fBm noise.
        /// Guarantees at least one tile of every Terrain type.
        /// </summary>
        /// <param name="map">Target map to fill.</param>
        /// <param name="seed">Seed for deterministic generation.</param>
        /// <param name="flavor">Continental (big landmasses) or Islands (archipelago).</param>
        /// <param name="waterLevel">
        /// Optional sea level [0..1]. If null, a default is chosen per flavor.
        /// Lower -> more land, Higher -> more water.
        /// </param>
        public static void GenerateWorld(
            Map map,
            int seed,
            WorldFlavor flavor = WorldFlavor.Continental,
            double? waterLevel = null)
        {
            // Flavor presets
            int octaves = flavor == WorldFlavor.Continental ? 4 : 5;
            double lacunarity = flavor == WorldFlavor.Continental ? 2.0 : 2.2;
            double persistence = flavor == WorldFlavor.Continental ? 0.50 : 0.55;
            double sea = waterLevel ?? (flavor == WorldFlavor.Continental ? 0.53 : 0.62);
            double coastBand = 0.04; // narrow band around sea level for Coast
            double hillThreshold = 0.68; // land height to hills
            double mountThreshold = 0.80; // hills to mountains

            // Independent moisture field
            int moistOctaves = octaves - 1;
            double moistLac = lacunarity;
            double moistPers = 0.6;

            // Precompute random phases per octave so output is deterministic and cheap
            var phasesH = MakePhases(seed * 101 + 17, octaves);
            var phasesM = MakePhases(seed * 313 + 29, Math.Max(1, moistOctaves));

            var counts = new Dictionary<Terrain, int>();
            foreach (Terrain t in Enum.GetValues(typeof(Terrain)))
                counts[t] = 0;

            // Assign tiles
            for (int r = 0; r < map.Height; r++)
            {
                int qStart = Map.QStart(r);
                for (int c = 0; c < map.Width; c++)
                {
                    var h = map.FromColRow(c, r);

                    // Periodic height and moisture in [0,1]
                    double height = FBMPeriodic(c, r, map.Width, map.Height, phasesH, octaves, lacunarity, persistence);
                    double moist = FBMPeriodic(c, r, map.Width, map.Height, phasesM, Math.Max(1, moistOctaves), moistLac, moistPers);

                    // Slight flavor shaping (subtle; preserves wrap)
                    if (flavor == WorldFlavor.Islands)
                    {
                        // "Ridged" enhancement for more islands
                        double ridged = 1.0 - Math.Abs(2.0 * height - 1.0);
                        height = 0.6 * height + 0.4 * ridged;
                    }
                    else
                    {
                        // Gentle smoothing for continents
                        height = 0.7 * height + 0.3 * SmoothStep(height);
                    }

                    Terrain t;
                    if (height < sea - coastBand - 0.02) t = Terrain.Ocean;
                    else if (height < sea + coastBand) t = Terrain.Coast;
                    else if (height > mountThreshold) t = Terrain.Mountains;
                    else if (height > hillThreshold) t = Terrain.Hills;
                    else
                    {
                        // Biomes by moisture on lowlands
                        if (moist < 0.18) t = Terrain.Desert;
                        else if (moist < 0.35) t = Terrain.Plains;
                        else if (moist < 0.55) t = Terrain.Grassland;
                        else if (moist < 0.80) t = Terrain.Forest;
                        else t = Terrain.Tundra;
                    }

                    map[h] = new Tile(t);
                    counts[t]++;
                }
            }

            // Ensure every terrain appears at least once.
            EnsureAllTerrainsPresent(map, counts, seed);
        }

        // ---------- Helpers ----------

        private static (double[] px, double[] py) MakePhases(int seed, int octaves)
        {
            var rng = new Random(seed);
            var px = new double[octaves];
            var py = new double[octaves];
            for (int i = 0; i < octaves; i++)
            {
                px[i] = rng.NextDouble() * Math.PI * 2.0;
                py[i] = rng.NextDouble() * Math.PI * 2.0;
            }
            return (px, py);
        }

        /// <summary>
        /// Periodic fBm based on separable sin/cos waves, guaranteed to tile across width/height.
        /// Returns [0,1].
        /// </summary>
        private static double FBMPeriodic(
            int x, int y, int width, int height,
            (double[] px, double[] py) phases,
            int octaves, double lacunarity, double persistence)
        {
            double twoPi = Math.PI * 2.0;
            double value = 0.0;
            double amp = 1.0;
            double sumAmp = 0.0;
            double fx = 1.0;
            double fy = 1.0;

            for (int i = 0; i < octaves; i++)
            {
                // Use integer cycle counts so the field is perfectly periodic
                int cycX = Math.Max(1, (int)Math.Round(fx));
                int cycY = Math.Max(1, (int)Math.Round(fy));

                double ax = twoPi * cycX * x / width + phases.px[i];
                double ay = twoPi * cycY * y / height + phases.py[i];

                // Blend a few simple, fast basis functions
                double basis = 0.3333333 * (Math.Sin(ax) + Math.Cos(ay) + Math.Sin(ax + ay));
                value += amp * basis;
                sumAmp += amp;

                amp *= persistence;
                fx *= lacunarity;
                fy *= lacunarity;
            }

            // [-1,1] -> [0,1]
            return 0.5 * (value / sumAmp + 1.0);
        }

        private static double SmoothStep(double t)
        {
            // classic smoothstep on [0,1]
            t = Math.Clamp(t, 0.0, 1.0);
            return t * t * (3.0 - 2.0 * t);
        }

        private static void EnsureAllTerrainsPresent(Map map, Dictionary<Terrain, int> counts, int seed)
        {
            var rng = new Random();
            // Quick helpers
            Hex? FindOceanNearLand()
            {
                foreach (var h in map.AllHexes())
                {
                    if (map[h].Terrain != Terrain.Ocean) continue;
                    foreach (var n in map.Neighbors(h))
                        if (map[n].Terrain != Terrain.Ocean) return h;
                }
                return null;
            }

            Hex? FindLandNearOcean()
            {
                foreach (var h in map.AllHexes())
                {
                    if (map[h].Terrain == Terrain.Ocean) continue;
                    foreach (var n in map.Neighbors(h))
                        if (map[n].Terrain == Terrain.Ocean) return h;
                }
                return null;
            }

            // Ensure Ocean
            if (counts[Terrain.Ocean] == 0)
            {
                var cand = FindLandNearOcean() ?? RandomAny(map, rng);
                map[cand] = new Tile(Terrain.Ocean);
                counts[Terrain.Ocean]++;
            }

            // Ensure Coast
            if (counts[Terrain.Coast] == 0)
            {
                var cand = FindOceanNearLand() ?? RandomAny(map, rng);
                map[cand] = new Tile(Terrain.Coast);
                counts[Terrain.Coast]++;
            }

            // Ensure each land biome
            EnsureOne(map, counts, Terrain.Mountains, h => IsLand(map[h].Terrain), rng);
            EnsureOne(map, counts, Terrain.Hills, h => IsLand(map[h].Terrain), rng);
            EnsureOne(map, counts, Terrain.Forest, h => IsLowland(map[h].Terrain), rng);
            EnsureOne(map, counts, Terrain.Plains, h => IsLowland(map[h].Terrain), rng);
            EnsureOne(map, counts, Terrain.Grassland, h => IsLowland(map[h].Terrain), rng);
            EnsureOne(map, counts, Terrain.Desert, h => IsLowland(map[h].Terrain), rng);
            EnsureOne(map, counts, Terrain.Tundra, h => IsLowland(map[h].Terrain), rng);
        }

        private static bool IsLand(Terrain t)
            => t != Terrain.Ocean && t != Terrain.Coast;

        private static bool IsLowland(Terrain t)
            => IsLand(t) && t != Terrain.Hills && t != Terrain.Mountains;

        private static void EnsureOne(
            Map map,
            Dictionary<Terrain, int> counts,
            Terrain target,
            Func<Hex, bool> predicate,
            Random rng)
        {
            if (counts[target] > 0) return;

            // Try to find a suitable candidate; fall back to any land
            var candidates = new List<Hex>();
            foreach (var h in map.AllHexes())
                if (predicate(h)) candidates.Add(h);

            if (candidates.Count == 0)
            {
                foreach (var h in map.AllHexes())
                    if (IsLand(map[h].Terrain)) { candidates.Add(h); break; }
            }

            if (candidates.Count > 0)
            {
                var pick = candidates[rng.Next(candidates.Count)];
                map[pick] = new Tile(target);
                counts[target]++;
            }
        }

        private static Hex RandomAny(Map map, Random rng)
        {
            int r = rng.Next(map.Height);
            int c = rng.Next(map.Width);
            return map.FromColRow(c, r);
        }
    }
}
