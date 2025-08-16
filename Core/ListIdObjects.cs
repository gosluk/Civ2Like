using Civ2Like.View.Core.Interfaces;
using System.Collections;

namespace Civ2Like.View.Core;

public sealed class ListIdObjects<T> : IList<T>
    where T : IIdObject
{
    // Internal backing list
    private readonly List<T> _items = new();

    public T this[Guid id]
        => this.FirstOrDefault(x => x.Id == id)
           ?? throw new KeyNotFoundException($"Unit with ID {id} not found.");

    public bool ContainsKey(Guid id) => this.Any(x => x.Id == id);

    private void Throw() => throw new ArgumentException("Duplicated item");

    public T this[int index]
    {
        get => _items[index];
        set
        {
            if (ContainsKey(value.Id))
            {
                Throw();
            }

            _items[index] = value;
        }
    }

    public int Count => _items.Count;

    public bool IsReadOnly => ((ICollection<T>)_items).IsReadOnly;

    public void Add(T item)
    {
        if (ContainsKey(item.Id))
        {
            Throw();
        }

        _items.Add(item);
    }

    public void Clear() => _items.Clear();

    public bool Contains(T item) => _items.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    public int IndexOf(T item) => _items.IndexOf(item);

    public void Insert(int index, T item) => _items.Insert(index, item);

    public bool Remove(T item) => _items.Remove(item);

    public void RemoveAt(int index) => _items.RemoveAt(index);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public List<T> ForEach(Action<T> action)
    {
        _items.ForEach(action);

        return _items;
    }
}
