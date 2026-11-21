using EmployeeApp.Core.Models;

namespace EmployeeApp.Core.Repositories;

/// <summary>
/// 3.1 Dictionary keyed by Id: O(1) lookup instead of scanning a List (O(n)).
/// 3.2 The constraint <c>IEntity</c> is what lets us read <c>item.Id</c> on a generic T.
/// </summary>
public class InMemoryRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly Dictionary<int, T> _items = new();

    public void Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);   // 1.6 guard clause

        // TryAdd returns false instead of throwing when the key exists (the Try pattern, 1.3 / 1.8)
        if (!_items.TryAdd(item.Id, item))
            throw new InvalidOperationException($"{typeof(T).Name} #{item.Id} already exists.");   // 3.2 typeof(T): reified generics
    }

    public T? GetById(int id) => _items.TryGetValue(id, out var item) ? item : null;

    /// <summary>
    /// Returns a copy so callers can't modify the store while looping over it (1.4 foreach rule).
    /// 3.1 A Dictionary does NOT promise any order: after Remove + Add it reuses freed slots, so
    /// "Load from file" used to list employees backwards. 3.4 OrderBy gives a stable, predictable order.
    /// </summary>
    public IReadOnlyList<T> GetAll() => _items.Values.OrderBy(item => item.Id).ToList();

    public void Update(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_items.ContainsKey(item.Id))
            throw new KeyNotFoundException($"{typeof(T).Name} #{item.Id} doesn't exist.");
        _items[item.Id] = item;   // same object: no-op; new object: replaces the old one
    }

    public bool Remove(int id) => _items.Remove(id);
}
