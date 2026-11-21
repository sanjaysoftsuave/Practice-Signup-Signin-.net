using EmployeeApp.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeApp.Infrastructure.Data;

/// <summary>
/// 9.1 The DbContext is EF Core's "session" with the database (Java: JPA EntityManager / Hibernate Session).
/// Each DbSet is one table you can query with LINQ (3.5: it's an IQueryable, so LINQ becomes SQL).
/// Registered as SCOPED: one DbContext per HTTP request. It is not thread-safe, so never a singleton.
/// </summary>
public class EmployeeDbContext(DbContextOptions<EmployeeDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AppUser> Users => Set<AppUser>();   // 10.1 login accounts

    /// <summary>9.2 Table and column rules live in the Configurations folder, one class per entity.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmployeeDbContext).Assembly);
}
