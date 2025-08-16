using Civ2Like.Core;

namespace Civ2Like.Views.Events;
public class PlayerSelectionChangedEvent
{
    public PlayerSelectionChangedEvent(Game game, Player player)
    {
        Player = player;
        NumberOfUnits = (uint)game.Units.Count(u => u.Player == player);
        NumberOfCities = (uint)game.Cities.Count(c => c.Player == player);
        Turn = game.Turn;
    }

    public Player Player { get; }

    public uint NumberOfUnits { get; }

    public uint NumberOfCities { get; }

    public uint Turn { get; }
}
