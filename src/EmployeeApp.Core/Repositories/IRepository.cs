using EmployeeApp.Core.Models;

namespace EmployeeApp.Core.Repositories;

/// <summary>
/// 2.6 Dependency Inversion: services depend on this abstraction, not on a concrete store,
/// so a SQL version (Week 2) or a fake (Day 4 tests) can be swapped in.
/// 3.2 Generic with constraints: one contract for Employee, Department, …
/// </summary>
public interface IRepository<T> where T : class, IEntity
{
    void Add(T item);
    T? GetById(int id);
    IReadOnlyList<T> GetAll();

    /// <summary>
    /// Saves changes to an item that already exists (same Id). <paramref name="item"/> can be the stored
    /// object after it was changed, or a brand-new object that replaces it.
    /// Week 2: an in-memory Dictionary didn't need this (changing the object WAS saving it),
    /// but a database has to be told. Throws <see cref="KeyNotFoundException"/> if the Id doesn't exist.
    /// </summary>
    void Update(T item);

    bool Remove(int id);
}
