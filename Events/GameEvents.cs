using System;

namespace Civ2Like.View
{
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

    public sealed class CityProductionProcessed : IGameEvent
    {
        public int Progress { get; set; }

        public Guid CityId { get; set; }

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

    internal class UnitCreatedEvent : IGameEvent
    {
        public Guid CityId { get; set; }

        public void Apply(Game game)
        {
            var pos = game.Cities[CityId].Pos;
            var player = game.Cities[CityId].Owner;

            game.Units.Add(new Unit(player, pos, MovementPreset.Land));
        }
    }

    public sealed class CityFoundedEvent : IGameEvent
    {
        public Guid PlayerId { get; set; }
        public Guid CityId { get; set; }
        public string Name { get; set; } = "City";
        public int Q { get; set; }
        public int R { get; set; }
        public DateTime Utc { get; set; } = DateTime.UtcNow;

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

    public sealed class TurnEndedEvent : IGameEvent
    {
        public int NewActiveIndex { get; set; }
        public int NewTurn { get; set; }
        public DateTime Utc { get; set; } = DateTime.UtcNow;
        public void Apply(Game game)
        {
            game.ActiveIndex = NewActiveIndex;
            game.Turn = NewTurn;

            game.Events.Process(game, game.Cities.SelectMany(c => c.EndOfTurnEvents()).ToArray());

            foreach (var u in game.Units)
            {
                if (u.Owner == game.ActivePlayer)
                {
                    u.MovesLeft = u.MoveAllowance;
                }
            }

            game.SelectedUnit = game.Units.First(u => u.Owner == game.ActivePlayer);

            //game.Events.Process(game, new TurnEndedEvent { NewActiveIndex = NewActiveIndex, NewTurn = NewTurn });
        }
    }
}