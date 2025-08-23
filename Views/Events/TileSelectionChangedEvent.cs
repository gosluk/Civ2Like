using Civ2Like.Core;
using Civ2Like.Hexagon;

namespace Civ2Like.Views.Events;

public sealed class TileSelectionChangedEvent
{
    public TileSelectionChangedEvent(Game game, Hex? position)
    {
        Game = game;
        Position = position;
    }

    public Hex? Position { get; }

    public Game Game { get; }
}
