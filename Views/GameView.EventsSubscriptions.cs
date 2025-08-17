using Civ2Like.Config;
using Civ2Like.Core;
using Civ2Like.Core.Units;
using Civ2Like.Hexagon;
using Civ2Like.Views.Events;
using CommunityToolkit.Mvvm.Messaging;

namespace Civ2Like.Views;
public sealed partial class GameView : IRecipient<CenterOnUnitEvent>
{
    public void Receive(CenterOnUnitEvent message)
    {
        Unit unit = _game.Units[message.UnitId];

        // Normalize to canonical world coords
        Hex world = _game.Map.Canonical(unit.Pos);

        // Choose the screen "center" cell
        int centerRow = GameConfig.Height / 2;
        int centerCol = GameConfig.Width / 2;

        // Column index of the world hex within its row
        int worldCol = world.Q - Map.QStart(world.R);

        // Offsets so that WorldHexAtScreen(centerCol, centerRow) == world
        _viewRowOffset = Mod(world.R - centerRow, GameConfig.Height);
        _viewColOffset = Mod(worldCol - centerCol, GameConfig.Width);

        // Shift to avoid anomaly
        if (_viewRowOffset % 2 == 1)
        {
            _viewRowOffset++;
        }

        InvalidateVisual();
    }
}
