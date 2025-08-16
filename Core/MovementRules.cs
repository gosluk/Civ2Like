using System.Collections.Generic;

namespace Civ2Like.View
{
    public enum MovementPreset { Land, Naval, Flying }

    public sealed class MovementRules
    {
        private readonly HashSet<Terrain> _impassable = new();
        private readonly Dictionary<Terrain, int> _cost = new();

        public MovementRules Block(params Terrain[] types) { foreach (var t in types) _impassable.Add(t); return this; }
        public MovementRules Cost(Terrain t, int c) { _cost[t] = c; return this; }

        public bool CanEnter(Terrain t) => !_impassable.Contains(t);
        public int MoveCost(Terrain t)
        {
            if (!CanEnter(t)) return 9999;
            if (_cost.TryGetValue(t, out var c)) return c;
            return t switch
            {
                Terrain.Mountains => 3,
                Terrain.Hills or Terrain.Forest => 2,
                _ => 1
            };
        }

        public static MovementRules FromPreset(MovementPreset preset)
        {
            switch (preset)
            {
                case MovementPreset.Land:
                    return new MovementRules().Block(Terrain.Ocean).Block(Terrain.Coast).Cost(Terrain.Forest, 2).Cost(Terrain.Hills, 2).Cost(Terrain.Mountains, 3);
                case MovementPreset.Naval:
                    return new MovementRules()
                        .Block(Terrain.Grassland, Terrain.Plains, Terrain.Forest, Terrain.Hills, Terrain.Mountains, Terrain.Desert, Terrain.Tundra)
                        .Cost(Terrain.Ocean, 1).Cost(Terrain.Coast, 1);
                case MovementPreset.Flying:
                    return new MovementRules();
                default:
                    return new MovementRules();
            }
        }

        public static MovementRules LandOnly() => FromPreset(MovementPreset.Land);
        public static MovementRules Amphibious() => new MovementRules();
    }
}
