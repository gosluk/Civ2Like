using Avalonia.Media;
using Civ2Like.Core.Nations;
using Civ2Like.Core.Players;
using Civ2Like.Core.Units;
using Civ2Like.Core.World;
using Civ2Like.Events.Items;
using System.Reflection;

namespace Civ2Like.Core;

public sealed partial class Game
{
    private void InitializeTestSetup()
    {
        RandomizeNations();

        RandomizePlayer(FactionNameGenerator.Next());
        RandomizePlayer(FactionNameGenerator.Next());

        InitializeUnitTypes();

        foreach(var px in Players.Select((p, index) => new {p, index }))
        {
            RandomizeStart(px.p, px.index);
        }
    }

    private void InitializeUnitTypes()
    {
        UnitTypes.Add(new UnitType()
        {
            MaxHealth = 10,
            MoveAllowance = 2,
            Rules = MovementRules.LandOnly(),
            TileVisibility = 1,
            Name = "Solider",
            AttackRange = 0,
            AttackRanged = 0,
            AttackMelee = 1,
            DefenseRanged = 1,
            DefenseMelee = 1,
        });

        UnitTypes.Add(new UnitType()
        {
            MaxHealth = 5,
            MoveAllowance = 2,
            Rules = MovementRules.LandOnly(),
            TileVisibility = 1,
            Name = "Settler",
            AttackRange = 0,
            AttackRanged = 0,
            AttackMelee = 0,
            DefenseRanged = 1,
            DefenseMelee = 1,
        });

        UnitTypes.Add(new UnitType()
        {
            MaxHealth = 10,
            MoveAllowance = 3,
            Rules = MovementRules.LandOnly(),
            TileVisibility = 1,
            Name = "Runner",
            AttackRange = 0,
            AttackRanged = 0,
            AttackMelee = 2,
            DefenseRanged = 1,
            DefenseMelee = 2,
        });

        UnitTypes.Add(new UnitType()
        {
            MaxHealth = 10,
            MoveAllowance = 1,
            Rules = MovementRules.LandOnly(),
            TileVisibility = 1,
            Name = "Defender",
            AttackRange = 1,
            AttackRanged = 2,
            AttackMelee = 2,
            DefenseRanged = 1,
            DefenseMelee = 2,
        });

        UnitTypes.Add(new UnitType()
        {
            MaxHealth = 10,
            MoveAllowance = 1,
            Rules = MovementRules.NavalOnly(),
            TileVisibility = 1,
            Name = "Ship",
            AttackRange = 1,
            AttackRanged = 2,
            AttackMelee = 2,
            DefenseRanged = 1,
            DefenseMelee = 2,
        });
    }

    private void RandomizeNations()
    {
        for (int i = 0; i < 20; i++)
        {
            Nations.Add(new Nation()
            {
                Name = NationNameGenerator.Next(),
                Ideology = new IdeologyProfile()
                {
                    EgalitarianVsAuthority = _rng.NextDouble(),
                    PacifistVsMilitarist = _rng.NextDouble(),
                    MaterialistVsSpiritual = _rng.NextDouble(),
                    XenophileVsXenophobe = _rng.NextDouble(),
                },
            });
        }
    }

    private void RandomizePlayer(string name)
    {
        var brushes = typeof(Colors).
            GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).
            ToArray();
        Avalonia.Media.Color Get() => (Avalonia.Media.Color)brushes[new Random().Next(brushes.Length)].GetValue(null)!;

        Players.Add(new Player(Guid.NewGuid())
        {
            Name = name,
            ColorA = Get(),
            ColorB = Get(),
            Founder = Nations[_rng.Next(Nations.Count - 1)],
        });
    }

    private void RandomizeStart(Player player, int index)
    {
        Terrain[] notAllowed = [Terrain.Ocean, Terrain.Coast, Terrain.Mountains, Terrain.Desert];
        var start = Map.AllHexes().Where(h => !notAllowed.Contains(Map[h].Terrain) && !Units.Select(i => i.Pos).Contains(h)).Skip(60 + index * 3).FirstOrDefault();
        if (start == default)
        {
            throw new NotImplementedException("Can not find start location for player " + player.Name);
        }

        var unit = new Unit(player, start, UnitTypes.First())
        {
            Name = UnitNameGenerator.Next(),
        };
        Units.Add(unit);

        Events.Process(this,
            new UnitCreatedEvent()
            {
                PlayerId = player.Id,
                Pos = start,
                UnitTypeId = UnitTypes.First().Id,
            },
            new PlayerAcquireTile()
            {
                PlayerId = player.Id,
                Pos = start,
            },
            new CityFoundedEvent()
            {
                PlayerId = player.Id,
                CityId = Guid.NewGuid(),
                Name = CityNameGenerator.Next(),
                Q = start.Q,
                R = start.R
            });
    }
}
