using System.Text.Json.Serialization;

namespace Civ2Like.View
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(System.Collections.Generic.List<IGameEvent>))]
    internal partial class Civ2LikeJsonContext : JsonSerializerContext { }
}
