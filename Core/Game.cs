using Avalonia.Media;
using Civ2Like.Events;
using Civ2Like.Hexagon;
using Civ2Like.View;
using Civ2Like.View.Core;
using DynamicData;
using System.Reflection;

namespace Civ2Like.Core;

public sealed class Game
{
    private readonly Random _rng;
    private int _unitIdCounter = 1;

    public Map Map { get; }

    public EventProcessor Events { get; }

    public ListIdObjects<Player> Players { get; } = new();

    public ListIdObjects<Unit> Units   { get; } = new();

    public ListIdObjects<City>   Cities  { get; } = new();

    public int Turn        { get; internal set; } = 1;
    public int ActiveIndex { get; internal set; } = 0;

    public Player ActivePlayer => Players[ActiveIndex];
    public Unit?  SelectedUnit { get; internal set; }

    public Game(int width, int height, int seed, EventProcessor? processor = null)
    {
        _rng = new Random(seed);
        Map  = new Map(width, height);
        Events = processor ?? new EventProcessor();

        //GenerateWorld();
        MapGeneration.ImproveGame.GenerateWorld(Map, _rng.Next() + 400, MapGeneration.WorldFlavor.Islands);

        int row = height / 2;
        Hex left  = Map.FromColRow(0, row);
        Hex right = Map.FromColRow(width - 1, row);

        left  = FindNearestLandAlongRow(left, +1);
        right = FindNearestLandAlongRow(right, -1);

        RandomizePlayer("Reds");
        RandomizePlayer("Whites");

        Players.ForEach(RandomizeStart);

        Events.Process(this, new GameStartedEvent { Width = width, Height = height, Seed = seed });
    }

    private void RandomizePlayer(string name)
    {
        var brushes = typeof(Colors).
            GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).
            ToArray();
        Avalonia.Media.Color Get() => (Avalonia.Media.Color)brushes[_rng.Next(brushes.Length)].GetValue(null)!;

        Players.Add(new Player(Guid.NewGuid())
        {
            Name = name,
            ColorA = Get(),
            ColorB = Get(),
        });
    }

    private void RandomizeStart(Player player)
    {
        Terrain[] notAllowed = [Terrain.Ocean, Terrain.Coast, Terrain.Mountains, Terrain.Desert];
        var start = Map.AllHexes().Where(h => !notAllowed.Contains(Map[h].Terrain)).OrderBy(_ => _rng.Next()).FirstOrDefault();
        if (start == default)
        {
            throw new NotImplementedException("Can not find start location for player " + player.Name);
        }

        var unit = new Unit(player, start, MovementPreset.Land) { Id = Guid.NewGuid() };
        Units.Add(unit);
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
            if (u.Player == ActivePlayer)
            {
                u.MovesLeft = u.MoveAllowance;
            }
        }

        SelectedUnit = Units.First(u => u.Player == ActivePlayer);
        Events.Process(this, new TurnEndedEvent { NewActiveIndex = ActiveIndex, NewTurn = Turn });
    }

    public bool TrySelectUnitAt(Hex h)
    {
        h = Map.Canonical(h);
        foreach (var u in Units)
        {
            if (u.Pos == h && u.Player == ActivePlayer && u.MovesLeft > 0)
            {
                SelectedUnit = u;
                return true;
            }
        }
        return false;
    }
}
