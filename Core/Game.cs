using Civ2Like.Core;
using DynamicData;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Civ2Like
{
    public sealed class Game
    {
        private readonly Random _rng;
        private int _unitIdCounter = 1;

        public Map Map { get; }
        public EventProcessor Events { get; }

        public ListId<Player> Players { get; } = new();

        public ListId<Unit> Units   { get; } = new();

        public ListId<City>   Cities  { get; } = new();

        public int Turn        { get; internal set; } = 1;
        public int ActiveIndex { get; internal set; } = 0;

        public Player ActivePlayer => Players[ActiveIndex];
        public Unit?  SelectedUnit { get; internal set; }

        public Game(int width, int height, int seed, EventProcessor? processor = null)
        {
            _rng = new Random(seed);
            Map  = new Map(width, height);
            Events = processor ?? new EventProcessor();

            Players.Add(new Player(Guid.NewGuid()));
            Players.Add(new Player(Guid.NewGuid()));

            GenerateWorld();
            ////sMapGeneration.GenerateWorld_BigContinents(Map);

            int row = height / 2;
            Hex left  = Map.FromColRow(0, row);
            Hex right = Map.FromColRow(width - 1, row);

            left  = FindNearestLandAlongRow(left, +1);
            right = FindNearestLandAlongRow(right, -1);

            var u0 = new Unit(Players[0], left,  MovementPreset.Land) { Id = Guid.NewGuid() };
            var u1 = new Unit(Players[1], right, MovementPreset.Land) { Id = Guid.NewGuid() };
            Units.Add(u0);
            Units.Add(u1);
            SelectedUnit = u0;

            Events.Process(this, new GameStartedEvent { Width = width, Height = height, Seed = seed });
        }

        private Hex FindNearestLandAlongRow(Hex start, int dir)
        {
            int r = start.R;
            int qStart = Map.QStart(r);
            int c0 = start.Q - qStart;

            for (int k = 0; k < Map.Width; k++)
            {
                int c = (c0 + dir * k) % Map.Width; if (c < 0) c += Map.Width;
                var h = Map.FromColRow(c, r);
                if (Map[h].Terrain != Terrain.Ocean) return h;
            }
            return start;
        }

        private void GenerateWorld()
        {
            var noise = new Dictionary<Hex, double>();
            foreach (var h in Map.AllHexes()) noise[h] = _rng.NextDouble();

            for (int it = 0; it < 2; it++)
            {
                var next = new Dictionary<Hex, double>(noise.Count);
                foreach (var h in Map.AllHexes())
                {
                    double sum = noise[h]; int cnt = 1;
                    foreach (var n in Map.Neighbors(h)) { sum += noise[n]; cnt++; }
                    next[h] = sum / cnt;
                }
                noise = next;
            }

            foreach (var h in Map.AllHexes())
            {
                double v = noise[h];
                var t = v < 0.38 ? Terrain.Ocean :
                        v < 0.46 ? Terrain.Coast :
                        v < 0.60 ? Terrain.Grassland :
                        v < 0.70 ? Terrain.Plains :
                        v < 0.80 ? Terrain.Forest :
                        v < 0.88 ? Terrain.Hills :
                                   Terrain.Mountains;
                Map[h] = new Tile(t);
            }
        }

        private static int ToroidalHeuristic(Map map, Hex a, Hex b)
        {
            int best = int.MaxValue;
            var a_ax = a;
            int colB = b.Q - Map.QStart(b.R);
            int[] rowShifts = { -map.Height, 0, map.Height };
            int[] colShifts = { -map.Width, 0, map.Width };
            foreach (var dr in rowShifts)
            {
                int rr = b.R + dr;
                foreach (var dc in colShifts)
                {
                    int cc = colB + dc;
                    int qb = Map.QStart(rr) + cc;
                    var b_ax = new Hex(qb, rr);
                    int d = Hex.Distance(a_ax, b_ax);
                    if (d < best) best = d;
                }
            }
            return best;
        }

        public IReadOnlyList<Hex> FindPath(Hex start, Hex goal, MovementRules rules)
        {
            start = Map.Canonical(start);
            goal  = Map.Canonical(goal);

            var open = new PriorityQueue<Hex, int>();
            var came = new Dictionary<Hex, Hex>();
            var g = new Dictionary<Hex, int> { [start] = 0 };
            open.Enqueue(start, 0);

            while (open.Count > 0)
            {
                var cur = open.Dequeue();
                if (cur == goal) break;

                foreach (var n in Map.Neighbors(cur))
                {
                    int cost = rules.MoveCost(Map[n].Terrain);
                    if (cost >= 9999) continue;
                    int ng = g[cur] + cost;
                    if (!g.TryGetValue(n, out var old) || ng < old)
                    {
                        g[n] = ng;
                        int h = ToroidalHeuristic(Map, n, goal);
                        open.Enqueue(n, ng + h);
                        came[n] = cur;
                    }
                }
            }

            var path = new List<Hex>();
            if (!came.ContainsKey(goal) && start != goal) return path;
            var c2 = goal; path.Add(c2);
            while (c2 != start)
            {
                if (!came.TryGetValue(c2, out var prev)) break;
                c2 = prev; path.Add(c2);
            }
            path.Reverse();
            return path;
        }

        public IReadOnlyList<Hex> FindPath(Hex start, Hex goal) => SelectedUnit is null ? [] : FindPath(start, goal, SelectedUnit.Rules);

        public void FollowPath(IReadOnlyList<Hex> path, MovementRules rules)
        {
            if (SelectedUnit is null || path.Count < 2)
            {
                return;
            }
           
            int idx = path.IndexOf(SelectedUnit.Pos);
            if (idx < 0)
            {
                idx = 0;
            }

            for (int i = idx + 1; i < path.Count; i++)
            {
                var step = Map.Canonical(path[i]);
                int cost = rules.MoveCost(Map[step].Terrain);
                if (SelectedUnit.MovesLeft - cost < 0)
                {
                    break;
                }

                var from = SelectedUnit.Pos;
                var to = step;
                SelectedUnit.Pos = to;
                SelectedUnit.MovesLeft -= cost;

                Events.Process(this, new UnitMovedEvent { UnitId = SelectedUnit.Id, FromQ = from.Q, FromR = from.R, ToQ = to.Q, ToR = to.R });
            }
        }

        public void FollowPath(IReadOnlyList<Hex> path) => FollowPath(path, SelectedUnit?.Rules ?? MovementRules.LandOnly());

        public bool TryFoundCity()
        {
            if (SelectedUnit is null) return false;
            if (Cities.Any(c => c.Pos == SelectedUnit.Pos))
            {
                return false;
            }

            var id = Guid.NewGuid();
            var name = $"City {id.ToString("N").Substring(0, 5)}";
            var pos = SelectedUnit.Pos;
            Events.Process(this, new CityFoundedEvent { PlayerId = ActivePlayer.Id, CityId = id, Name = name, Q = pos.Q, R = pos.R });
            return true;
        }

        public void EndTurn()
        {
            ActiveIndex = (ActiveIndex + 1) % Players.Count;

            if (ActiveIndex == 0) Turn++;

            foreach (var u in Units)
            {
                if (u.Owner == ActivePlayer)
                {
                    u.MovesLeft = u.MoveAllowance;
                }
            }

            SelectedUnit = Units.First(u => u.Owner == ActivePlayer);
            Events.Process(this, new TurnEndedEvent { NewActiveIndex = ActiveIndex, NewTurn = Turn });
        }

        public bool TrySelectUnitAt(Hex h)
        {
            h = Map.Canonical(h);
            foreach (var u in Units)
            {
                if (u.Pos == h && u.Owner == ActivePlayer) { SelectedUnit = u; return true; }
            }
            return false;
        }
    }
}
