using Civ2Like.Core;

namespace Civ2Like.Events.Items;

public sealed class TurnEndedEvent : IGameEvent
{
    public required int NewActiveIndex { get; init; }
    public required uint NewTurn { get; init; }

    public void Apply(Game game)
    {
        game.ActiveIndex = NewActiveIndex;
        game.Turn = NewTurn;

        game.ProcessEvent(game.Cities.SelectMany(c => c.EndOfTurnEvents()).ToArray());

        foreach (var u in game.Units)
        {
            if (u.Player == game.ActivePlayer)
            {
                u.MovesLeft = u.UnitType.MoveAllowance;
            }
        }

        foreach (var nation in game.Nations)
        {
            Core.Mechanics.Nations.ApplyGrowth(nation, game.Map.MapData);
            Core.Mechanics.Nations.ApplyMigration(nation, game.Map);
        }
    }
}