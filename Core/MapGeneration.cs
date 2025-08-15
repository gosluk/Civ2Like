using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Civ2Like.Core;

internal static class MapGeneration
{
    private static Random _rng = new Random();

    public static void GenerateWorld_ManySmallIslands(Map Map)
    {
        // --- build two bands of "noise": low-frequency (ocean mask) and high-frequency (island detail)
        var low = new Dictionary<Hex, double>();   // very smooth → continents/ocean bias
        var hi = new Dictionary<Hex, double>();   // slightly smooth → small islands

        foreach (var h in Map.AllHexes())
        {
            low[h] = _rng.NextDouble();
            hi[h] = _rng.NextDouble();
        }

        // heavier smoothing for "low" to get broad oceans
        low = Smooth(low, iterations: 6);
        // light smoothing for island detail
        hi = Smooth(hi, iterations: 1);

        // combine: high-frequency detail *against* a low-frequency ocean mask
        var combined = new Dictionary<Hex, double>(low.Count);
        foreach (var h in Map.AllHexes())
        {
            // center both fields roughly around 0.5, then compose
            double L = Clamp01(low[h]);
            double H = Clamp01(hi[h]);

            // push more water overall; islands pop where H outcompetes L
            // tweak weights to taste; (0.70, 0.35, -0.05) gives lots of small islands
            double v = H * 0.70 - L * 0.35 + 0.50 - 0.05;

            combined[h] = Clamp01(v);
        }

        // paint tiles with slightly water‑heavier thresholds
        foreach (var h in Map.AllHexes())
        {
            double v = combined[h];
            var t = v < 0.40 ? Terrain.Ocean :
                    v < 0.48 ? Terrain.Coast :
                    v < 0.62 ? Terrain.Grassland :
                    v < 0.72 ? Terrain.Plains :
                    v < 0.82 ? Terrain.Forest :
                    v < 0.90 ? Terrain.Hills :
                               Terrain.Mountains;
            Map[h] = new Tile(t);
        }

        // --- local helpers
        Dictionary<Hex, double> Smooth(Dictionary<Hex, double> src, int iterations)
        {
            var cur = src;
            for (int it = 0; it < iterations; it++)
            {
                var next = new Dictionary<Hex, double>(cur.Count);
                foreach (var h in Map.AllHexes())
                {
                    double sum = cur[h]; int cnt = 1;
                    foreach (var n in Map.Neighbors(h)) { sum += cur[n]; cnt++; }
                    next[h] = sum / cnt;
                }
                cur = next;
            }
            return cur;
        }

        double Clamp01(double x) => x < 0 ? 0 : (x > 1 ? 1 : x);
    }

    // Drop-in replacement: produces a few large continents with readable coasts.
    public static void GenerateWorld_BigContinents(Map Map)
    {
        // Gather cells once
        var all = new List<Hex>();
        foreach (var h in Map.AllHexes()) all.Add(h);
        int N = all.Count;

        // === 1) Choose continent seeds (2–3) ===
        int seedCount = (_rng.Next(0, 100) < 65) ? 3 : 2;
        var seeds = PickSeedsFarApart(all, seedCount);

        // === 2) Multi-source BFS: distance to nearest seed + owner ===
        var owner = new Dictionary<Hex, int>(N);
        var dist = new Dictionary<Hex, int>(N);
        MultiSourceBfs(seeds, owner, dist);

        // Per-seed radius (controls continent size), scaled by map size
        double baseR = Math.Sqrt(N) * 0.33;          // ~big blobs; raise/lower for size
        var radii = new double[seedCount];
        for (int i = 0; i < seedCount; i++)
            radii[i] = baseR * Lerp(0.85, 1.25, _rng.NextDouble());  // slight variety

        // === 3) Edge falloff: seas near borders (gentle) ===
        var edgeDist = DistanceFromBorder(all);
        int maxEdgeD = 1;
        foreach (var kv in edgeDist) if (kv.Value > maxEdgeD) maxEdgeD = kv.Value;

        // === 4) Detail field (light texture for coasts) ===
        var detail = new Dictionary<Hex, double>(N);
        foreach (var h in all) detail[h] = _rng.NextDouble();
        detail = Smooth(detail, iterations: 2); // mild blur
                                                // center detail to ~[-0.5..+0.5] small amplitude
        double mean = 0.0; foreach (var h in all) mean += detail[h];
        mean /= N;
        foreach (var h in all) detail[h] = (detail[h] - mean) * 0.35;

        // === 5) Build macro height from seed distance + combine with detail + edge falloff ===
        var height = new Dictionary<Hex, double>(N);
        double minH = double.PositiveInfinity, maxH = double.NegativeInfinity;

        foreach (var h in all)
        {
            int i = owner[h];                // nearest seed id
            double R = radii[i];
            double d = dist[h];              // graph distance (hex steps)
                                             // radial falloff from continent core; shape with a smooth curve
            double core = 1.0 - (d / R);
            core = Clamp01(core);
            core = SmoothStep(core);         // softer shoulders -> broad interiors

            // gentle edge seas (compute [0..1] proximity to interior)
            double ed = (double)edgeDist[h] / Math.Max(1, maxEdgeD);
            double edgeFactor = 0.65 + 0.35 * SmoothStep(ed); // 0.65 near rim → 1.0 deep interior

            // combine: continents dominate; detail adds coastal texture
            double v = core * 0.88 + detail[h] * 0.12;
            v *= edgeFactor;

            height[h] = v;
            if (v < minH) minH = v;
            if (v > maxH) maxH = v;
        }

        // === 6) Normalize to [0..1] ===
        double range = Math.Max(1e-9, maxH - minH);
        foreach (var h in all) height[h] = (height[h] - minH) / range;

        // === 7) Quantile thresholds → stable biome ratios, regardless of RNG ===
        // Target fractions similar to your originals:
        // Ocean 0–36%, Coast 36–44%, Grass 44–60%, Plains 60–70%, Forest 70–80%, Hills 80–88%, Mtns 88–100%
        var values = new List<double>(N);
        foreach (var h in all) values.Add(height[h]);
        values.Sort();

        double qOcean = Percentile(values, 0.36);
        double qCoast = Percentile(values, 0.44);
        double qGrass = Percentile(values, 0.60);
        double qPlains = Percentile(values, 0.70);
        double qForest = Percentile(values, 0.80);
        double qHills = Percentile(values, 0.88);

        // First pass: classify by quantiles
        var terrain0 = new Dictionary<Hex, Terrain>(N);
        foreach (var h in all)
        {
            double v = height[h];
            var t = v < qOcean ? Terrain.Ocean :
                    v < qCoast ? Terrain.Coast :
                    v < qGrass ? Terrain.Grassland :
                    v < qPlains ? Terrain.Plains :
                    v < qForest ? Terrain.Forest :
                    v < qHills ? Terrain.Hills :
                                 Terrain.Mountains;
            terrain0[h] = t;
        }

        // === 8) Coastline enforcement pass (1-hex ring) ===
        var finalT = new Dictionary<Hex, Terrain>(terrain0);
        foreach (var h in all)
        {
            bool neighborOcean = false;
            bool neighborLand = false;
            foreach (var n in Map.Neighbors(h))
            {
                var tn = terrain0[n];
                if (tn == Terrain.Ocean) neighborOcean = true;
                else if (tn != Terrain.Coast) neighborLand = true; // any solid land
                if (neighborOcean && neighborLand) break;
            }

            if (terrain0[h] == Terrain.Ocean && neighborLand)
                finalT[h] = Terrain.Coast;
            else if (terrain0[h] != Terrain.Ocean && neighborOcean)
                finalT[h] = Terrain.Coast;
        }

        // === 9) Write back to the map ===
        foreach (var h in all)
            Map[h] = new Tile(finalT[h]);

        // -------------------- local helpers --------------------
        Dictionary<Hex, double> Smooth(Dictionary<Hex, double> src, int iterations)
        {
            var cur = src;
            for (int it = 0; it < iterations; it++)
            {
                var next = new Dictionary<Hex, double>(cur.Count);
                foreach (var h in all)
                {
                    double sum = cur[h]; int cnt = 1;
                    foreach (var n in Map.Neighbors(h)) { sum += cur[n]; cnt++; }
                    next[h] = sum / Math.Max(1, cnt);
                }
                cur = next;
            }
            return cur;
        }

        // Multi-source BFS: sets owner (nearest seed index) and distance in hex steps.
        void MultiSourceBfs(List<Hex> seedsIn, Dictionary<Hex, int> ownerOut, Dictionary<Hex, int> distOut)
        {
            var q = new Queue<Hex>(seedsIn.Count * 4);
            for (int i = 0; i < seedsIn.Count; i++)
            {
                var s = seedsIn[i];
                ownerOut[s] = i;
                distOut[s] = 0;
                q.Enqueue(s);
            }

            while (q.Count > 0)
            {
                var h = q.Dequeue();
                int o = ownerOut[h];
                int d = distOut[h];

                foreach (var n in Map.Neighbors(h))
                {
                    int nd = d + 1;
                    if (!distOut.TryGetValue(n, out int curD) || nd < curD)
                    {
                        distOut[n] = nd;
                        ownerOut[n] = o;
                        q.Enqueue(n);
                    }
                }
            }
        }

        // Distance from border cells (cells with <6 neighbors).
        Dictionary<Hex, int> DistanceFromBorder(List<Hex> cells)
        {
            var q = new Queue<Hex>();
            var dd = new Dictionary<Hex, int>(cells.Count);
            foreach (var h in cells)
            {
                int deg = 0; foreach (var _ in Map.Neighbors(h)) deg++;
                if (deg < 6) { dd[h] = 0; q.Enqueue(h); }
            }
            // if map wraps (all deg==6), we still want a non-zero falloff; seed a few random rim cells
            if (q.Count == 0)
            {
                for (int i = 0; i < Math.Max(1, cells.Count / 50); i++)
                {
                    var h = cells[_rng.Next(cells.Count)];
                    dd[h] = 0; q.Enqueue(h);
                }
            }

            while (q.Count > 0)
            {
                var h = q.Dequeue();
                int d = dd[h];
                foreach (var n in Map.Neighbors(h))
                {
                    int nd = d + 1;
                    if (!dd.TryGetValue(n, out int cur) || nd < cur)
                    {
                        dd[n] = nd; q.Enqueue(n);
                    }
                }
            }
            return dd;
        }

        // Pick seeds reasonably far apart via "farthest-of-samples"
        List<Hex> PickSeedsFarApart(List<Hex> cells, int k)
        {
            var picked = new List<Hex>(k);
            picked.Add(cells[_rng.Next(cells.Count)]);

            while (picked.Count < k)
            {
                Hex best = cells[_rng.Next(cells.Count)];
                int bestScore = -1;

                for (int tries = 0; tries < 60; tries++)
                {
                    var cand = cells[_rng.Next(cells.Count)];
                    int minD = int.MaxValue;

                    // compute rough graph distance to nearest already picked seed
                    // (BFS until we hit any picked seed)
                    var q = new Queue<Hex>();
                    var seen = new HashSet<Hex>();
                    q.Enqueue(cand); seen.Add(cand);
                    int depth = 0; bool found = false;

                    while (q.Count > 0 && !found && depth < 999999)
                    {
                        int layer = q.Count;
                        for (int i = 0; i < layer; i++)
                        {
                            var x = q.Dequeue();
                            // hit?
                            for (int s = 0; s < picked.Count; s++)
                            {
                                if (x.Equals(picked[s])) { found = true; break; }
                            }
                            if (found) { minD = depth; break; }

                            foreach (var n in Map.Neighbors(x))
                            {
                                if (seen.Add(n)) q.Enqueue(n);
                            }
                        }
                        depth++;
                    }

                    if (minD > bestScore) { bestScore = minD; best = cand; }
                }

                picked.Add(best);
            }
            return picked;
        }

        // Utilities
        static double Clamp01(double x) => x < 0 ? 0 : (x > 1 ? 1 : x);
        static double Lerp(double a, double b, double t) => a + (b - a) * t;
        static double SmoothStep(double x) { x = Clamp01(x); return x * x * (3 - 2 * x); }

        static double Percentile(List<double> sortedAscending, double p)
        {
            if (sortedAscending.Count == 0) return 0.0;
            p = Math.Max(0.0, Math.Min(1.0, p));
            double idx = p * (sortedAscending.Count - 1);
            int i0 = (int)Math.Floor(idx);
            int i1 = Math.Min(sortedAscending.Count - 1, i0 + 1);
            double t = idx - i0;
            return sortedAscending[i0] * (1.0 - t) + sortedAscending[i1] * t;
        }
    }

    public static void GenerateWorld_LowVarianceLand(Map Map)
    {
        var noise = new Dictionary<Hex, double>();
        foreach (var h in Map.AllHexes()) noise[h] = _rng.NextDouble();

        // heavy smoothing → very gentle gradients
        noise = Smooth(noise, iterations: 8);

        // compress variation toward 0.5 (shrink range)
        var flat = new Dictionary<Hex, double>(noise.Count);
        foreach (var h in Map.AllHexes())
        {
            double v = noise[h];
            // pull toward mid by 70%: v' = 0.5 + (v - 0.5) * 0.30
            v = 0.5 + (v - 0.5) * 0.30;
            flat[h] = Clamp01(v);
        }

        // thresholds that favor mid biomes; oceans/mountains rarer
        foreach (var h in Map.AllHexes())
        {
            double v = flat[h];
            var t = v < 0.28 ? Terrain.Ocean :
                    v < 0.40 ? Terrain.Coast :
                    v < 0.68 ? Terrain.Grassland :
                    v < 0.78 ? Terrain.Plains :
                    v < 0.86 ? Terrain.Forest :
                    v < 0.92 ? Terrain.Hills :
                               Terrain.Mountains;
            Map[h] = new Tile(t);
        }

        Dictionary<Hex, double> Smooth(Dictionary<Hex, double> src, int iterations)
        {
            var cur = src;
            for (int it = 0; it < iterations; it++)
            {
                var next = new Dictionary<Hex, double>(cur.Count);
                foreach (var h in Map.AllHexes())
                {
                    double sum = cur[h]; int cnt = 1;
                    foreach (var n in Map.Neighbors(h)) { sum += cur[n]; cnt++; }
                    next[h] = sum / cnt;
                }
                cur = next;
            }
            return cur;
        }

        double Clamp01(double x) => x < 0 ? 0 : (x > 1 ? 1 : x);
    }
}
