using Civ2Like.Core;

namespace Civ2Like.Events.Items;

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