using EmployeeApp.Core.Models;
using EmployeeApp.Core.Repositories;
using EmployeeApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EmployeeApp.Infrastructure.Repositories;

/// <summary>
/// 2.6 Dependency Inversion paying off: the same <see cref="IRepository{T}"/> contract as
/// InMemoryRepository, but backed by PostgreSQL. EmployeeService doesn't change at all.
/// Every write calls SaveChanges straight away, so each repository call is its own transaction.
/// (Synchronous for now, to keep the Week 1 interface; Day 9 discusses making it async.)
/// </summary>
public class EfRepository<T>(EmployeeDbContext db) : IRepository<T> where T : class, IEntity
{
    private DbSet<T> Set => db.Set<T>();

    public void Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (Set.Find(item.Id) is not null)
            throw new InvalidOperationException($"{typeof(T).Name} #{item.Id} already exists.");   // same as InMemoryRepository

        Set.Add(item);
        db.SaveChanges();   // INSERT
    }

    /// <summary>Find checks the objects already loaded in this DbContext first, then queries by primary key.</summary>
    public T? GetById(int id) => Set.Find(id);

    /// <summary>Tracked (not AsNoTracking) because the service changes these objects and then calls Update.</summary>
    public IReadOnlyList<T> GetAll() =>
        Set.OrderBy(item => EF.Property<int>(item, "Id")).ToList();   // SELECT ... ORDER BY id

    public void Update(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        T existing = Set.Find(item.Id)
            ?? throw new KeyNotFoundException($"{typeof(T).Name} #{item.Id} doesn't exist.");

        if (!ReferenceEquals(existing, item))
        {
            // A NEW object replaces the stored one (PUT). The DbContext can't track two objects with the
            // same key, so stop tracking the old one and track the new one as "Modified" (UPDATE every column).
            db.Entry(existing).State = EntityState.Detached;
            var entry = Set.Update(item);

            // EF does NOT rewrite the TPH discriminator (employee_type) on its own, so without this a
            // FullTime → Manager change would save every column except the type, and Priya would still
            // load as FullTime. Set it from the new object's class. (Found by a unit test.)
            var discriminator = entry.Metadata.FindDiscriminatorProperty();
            if (discriminator is not null)
            {
                var type = entry.Property(discriminator.Name);
                type.CurrentValue = entry.Metadata.GetDiscriminatorValue();
                type.IsModified = true;
            }
        }
        // Same object: the change tracker already noticed which properties changed (e.g. only monthly_salary).
        db.SaveChanges();   // UPDATE
    }

    public bool Remove(int id)
    {
        T? item = Set.Find(id);
        if (item is null)
            return false;

        Set.Remove(item);
        db.SaveChanges();   // DELETE
        return true;
    }
}
