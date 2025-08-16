using Civ2Like.Core;
using Civ2Like.Hexagon;

namespace Civ2Like.Events;

public sealed class GameStartedEvent : IGameEvent
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Seed { get; set; }
    public DateTime Utc { get; set; } = DateTime.UtcNow;
    public void Apply(Game game) { }
}

public sealed class UnitMovedEvent : IGameEvent
{
    public Guid UnitId { get; set; }
    public int FromQ { get; set; }
    public int FromR { get; set; }
    public int ToQ { get; set; }
    public int ToR { get; set; }
    public DateTime Utc { get; set; } = DateTime.UtcNow;
    public void Apply(Game game)
    {
        var u = game.Units.First(x => x.Id == UnitId);
        if (u == null) return;
        u.Pos = game.Map.Canonical(new Hex(ToQ, ToR));
    }
}

public sealed class CityProductionProgressedEvent : IGameEvent
{
    public Guid CityId { get; init; }

    public void Apply(Game game)
    {
        City city = game.Cities[CityId];

        if (city.Production > 1)
        {
            city.Production--;
        }
        else
        {
            city.SetProduction();
            game.Events.Process(game, new UnitCreatedEvent { CityId = city.Id });
        }
    }
}

public sealed class CityGrowthProgressedEvent : IGameEvent
{
    public Guid CityId { get; init; }

    public void Apply(Game game)
    {
        City city = game.Cities[CityId];

        if (city.Growth > 1)
        {
            city.Growth--;
        }
        else
        {
            city.SetGrowth();
            city.Population++;

            Hex? nextTile = FindNextTileToAcquire(game, city.Pos);

            if (nextTile is not null)
            {
                game.Events.Process(game, new PlayerAcquireTile { PlayerId = city.Player.Id, Pos = nextTile.Value });
            }

            game.Events.Process(game, new CityPopulationUpdateEvent { CityId = city.Id, PopulationChange = 1, });
        }
    }

    private Hex? FindNextTileToAcquire(Game game, Hex pos)
    {
        Random rand = new();

        // Level 1 neighbours
        foreach (var neighbour0 in game.Map.Neighbors(pos).OrderBy(_ => rand.Next()))
        {
            if (game.Map[neighbour0].Owner is null)
            {
                return neighbour0;
            }
        }

        // Level 2 neighbours
        foreach (var neighbour0 in game.Map.Neighbors(pos).OrderBy(_ => rand.Next()))
        {
            foreach (var neighbour1 in game.Map.Neighbors(neighbour0).OrderBy(_ => rand.Next()))
            {
                if (game.Map[neighbour1].Owner is null)
                {
                    return neighbour1;
                }
            }
        }

        // Level 3 neighbours
        foreach (var neighbour0 in game.Map.Neighbors(pos).OrderBy(_ => rand.Next()))
        {
            foreach (var neighbour1 in game.Map.Neighbors(neighbour0).OrderBy(_ => rand.Next()))
            {
                foreach (var neighbour2 in game.Map.Neighbors(neighbour1).OrderBy(_ => rand.Next()))
                {
                    if (game.Map[neighbour2].Owner is null)
                    {
                        return neighbour2;
                    }
                }
            }
        }

        return null;
    }
}

public sealed class CityPopulationUpdateEvent : IGameEvent
{
    public Guid CityId { get; init; }

    public int PopulationChange { get; init; }

    public void Apply(Game game)
    {
        City city = game.Cities[CityId];
    }
}

internal class UnitCreatedEvent : IGameEvent
{
    public Guid CityId { get; set; }

    public void Apply(Game game)
    {
        var pos = game.Cities[CityId].Pos;
        var player = game.Cities[CityId].Player;

        game.Units.Add(new Unit(player, pos, MovementPreset.Land)
        {
            Name = game.UnitNameGenerator.Next(),
        });
    }
}

public sealed class CityFoundedEvent : IGameEvent
{
    public Guid PlayerId { get; set; }
    public Guid CityId { get; set; }
    public string Name { get; set; } = "City";
    public int Q { get; set; }
    public int R { get; set; }

    public void Apply(Game game)
    {
        var owner = game.Players.First(p => p.Id == PlayerId) ?? game.ActivePlayer;

        var city = new City(owner, Name, new Hex(Q, R))
        {
            Id = CityId
        };

        if (!game.Cities.ContainsKey(CityId))
        {
            game.Cities.Add(city);
        }
    }
}

public sealed class PlayerAcquireTile : IGameEvent
{
    public Guid PlayerId { get; set; }
    
    public Hex Pos { get; set; }

    public void Apply(Game game)
    {
        game.Map[Pos].Owner = game.Players[PlayerId];
    }
}

public sealed class TurnEndedEvent : IGameEvent
{
    public int NewActiveIndex { get; set; }
    public uint NewTurn { get; set; }
    public DateTime Utc { get; set; } = DateTime.UtcNow;
    public void Apply(Game game)
    {
        game.ActiveIndex = NewActiveIndex;
        game.Turn = NewTurn;

        game.Events.Process(game, game.Cities.SelectMany(c => c.EndOfTurnEvents()).ToArray());

        foreach (var u in game.Units)
        {
            if (u.Player == game.ActivePlayer)
            {
                u.MovesLeft = u.MoveAllowance;
            }
        }

        //game.SelectedUnit = game.Units.First(u => u.Player == game.ActivePlayer);
    }
}