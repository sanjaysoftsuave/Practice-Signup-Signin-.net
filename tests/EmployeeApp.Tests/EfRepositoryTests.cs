using EmployeeApp.Core.Exceptions;
using EmployeeApp.Core.Models;
using EmployeeApp.Core.Services;
using EmployeeApp.Infrastructure.Data;
using EmployeeApp.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EmployeeApp.Tests;

/// <summary>
/// Checks the EF Core mapping and <see cref="EfRepository{T}"/> against a real (but in-memory) SQLite database,
/// so they run anywhere without PostgreSQL. The same model and repository code runs on PostgreSQL;
/// only the provider differs (SQL dialect and column types).
/// Each test reads back through a NEW DbContext, so the data really came from the database,
/// not from objects the first context still remembers.
/// 4.4 IDisposable: xUnit calls Dispose after each test, which closes the connection and deletes the database.
/// </summary>
public sealed class EfRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<EmployeeDbContext> _options;

    public EfRepositoryTests()
    {
        _connection.Open();   // an in-memory SQLite database lives as long as its connection stays open
        _options = new DbContextOptionsBuilder<EmployeeDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        using var db = NewContext();
        db.Database.EnsureCreated();
        SampleData.Seed(ServiceOver(db));
    }

    public void Dispose() => _connection.Dispose();

    private EmployeeDbContext NewContext() => new(_options);

    private static EmployeeService ServiceOver(EmployeeDbContext db) =>
        new(new EfRepository<Employee>(db), new EfRepository<Department>(db));

    [Fact]
    public void Seed_ReadBackInNewContext_RestoresSubclassLocationAndAddress()
    {
        using var db = NewContext();

        var staff = ServiceOver(db).GetEmployees();

        Assert.Equal(5, staff.Count);
        Assert.IsType<Manager>(staff[0]);                    // employee_type column → the right class
        Assert.IsType<Intern>(staff[2]);
        Employee priya = staff[1];
        Assert.IsType<Remote>(priya.Location);               // value conversion: "Remote" → new Remote()
        Assert.Equal(new Address("Chennai", "600001"), priya.Address);   // complex type: two columns → record
        Assert.Null(staff[0].Address);
    }

    [Fact]
    public void GiveRaise_ChangedSalaryIsSaved()
    {
        using (var db = NewContext())
            ServiceOver(db).GiveRaise(2, 10m);

        using var check = NewContext();
        Assert.Equal(99000m, ServiceOver(check).GetEmployee(2).MonthlySalary);
    }

    [Fact]
    public void Update_NewObjectWithDifferentRole_ChangesTheStoredType()
    {
        using (var db = NewContext())
        {
            var service = ServiceOver(db);
            service.GetEmployee(2);   // like the PUT action: the old Priya is now tracked by this context
            service.Update(new Manager { Id = 2, Name = "Priya", DepartmentCode = "SALES", MonthlySalary = 120000m });
        }

        using var check = NewContext();
        Employee priya = ServiceOver(check).GetEmployee(2);
        Assert.IsType<Manager>(priya);                       // was FullTimeEmployee
        Assert.Equal("SALES", priya.DepartmentCode);
        Assert.Null(priya.Address);                          // PUT replaces everything
    }

    [Fact]
    public void Hire_ThenFire_RowIsGone()
    {
        using (var db = NewContext())
        {
            var service = ServiceOver(db);
            service.Hire(new FullTimeEmployee { Id = 6, Name = "Divya", DepartmentCode = "hr", MonthlySalary = 70000m });
            service.Fire(6);
        }

        using var check = NewContext();
        Assert.Throws<EmployeeNotFoundException>(() => ServiceOver(check).GetEmployee(6));
    }

    [Fact]
    public void Add_DuplicateId_Throws()
    {
        using var db = NewContext();
        var repo = new EfRepository<Employee>(db);

        Assert.Throws<InvalidOperationException>(() =>
            repo.Add(new FullTimeEmployee { Id = 1, Name = "Copy", DepartmentCode = "ENG", MonthlySalary = 1m }));
    }

    [Fact]
    public void Remove_ManagerWithReports_DatabaseForeignKeyRefuses()
    {
        // Skips the service's own check on purpose, to prove the DATABASE also protects the relationship.
        using var db = NewContext();
        var repo = new EfRepository<Employee>(db);

        Assert.Throws<DbUpdateException>(() => repo.Remove(1));   // Priya and Arun still point at Ravi
    }
}
