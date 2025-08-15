using Civ2Like.Core.Interfaces;

namespace Civ2Like.Core;

public sealed class ListId<T> : List<T>
    where T : IIdObject
{
    public T this[Guid id]
    {
        get => this.FirstOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException($"Unit with ID {id} not found.");
    }

    public bool ContainsKey(Guid id) => this.Any(x => x.Id == id);
}
