using Civ2Like.Core;
using System.Text.Json;

namespace Civ2Like.Events;

public sealed class EventProcessor
{
    public List<IGameEvent> Log { get; } = new();
    public bool IsReplaying { get; private set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        TypeInfoResolver = Civ2LikeJsonContext.Default
    };

    public void Process(Game game, params IGameEvent[] e)
    {
        foreach (var ev in e)
        {
            ev.Apply(game);

            if (!IsReplaying)
            {
                Log.Add(ev);
            }
        }
    }

    public void Replay(Game game, IEnumerable<IGameEvent> events)
    {
        IsReplaying = true;
        foreach (var e in events)
        {
            e.Apply(game);
        }
        IsReplaying = false;
    }

    public string ToJson() => JsonSerializer.Serialize(Log, JsonOptions);

    public void LoadFromJson(string json)
    {
        Log.Clear();
        var list = JsonSerializer.Deserialize<List<IGameEvent>>(json, JsonOptions);
        if (list != null) Log.AddRange(list);
    }
}
