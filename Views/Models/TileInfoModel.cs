using Civ2Like.Core.World;
using Civ2Like.Hexagon;
using Civ2Like.Views.Events;
using CommunityToolkit.Mvvm.Messaging;
using System.Text;

namespace Civ2Like.Views.Models;
internal class TileInfoModel
    : BaseModel, IRecipient<TileSelectionChangedEvent>
{
    public TileInfoModel()
    {
        WeakReferenceMessenger.Default.Register(this);
    }

    private string? _text;
    public string? Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    public void Receive(TileSelectionChangedEvent message)
    {
        StringBuilder sb = new();

        if (message.Position is Hex position)
        {
            Tile tile = message.Game.Map[position];

            sb.AppendLine($"Position: {message.Position}");
            sb.AppendLine($"Terrain: {tile.Terrain}");
            sb.AppendLine();
            foreach (var population in tile.Populations)
            {
                sb.AppendLine($"Population: {population.Key.Name} ({population.Value.Value})");
            }
        }

        Text = sb.ToString();
    }
}
