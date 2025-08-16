using Civ2Like.Core;
using Civ2Like.Hexagon;

namespace Civ2Like.Events.Items;

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
