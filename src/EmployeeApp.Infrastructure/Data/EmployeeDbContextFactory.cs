using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EmployeeApp.Infrastructure.Data;

/// <summary>
/// Used ONLY by the `dotnet ef` command-line tool (design time), never by the running app.
/// `dotnet ef migrations add` doesn't connect to the database, so a placeholder connection string is enough.
/// `dotnet ef database update` needs the real one: pass it with --connection, or set the
/// ConnectionStrings__EmployeeDb environment variable. No password is ever stored in this file.
/// </summary>
public class EmployeeDbContextFactory : IDesignTimeDbContextFactory<EmployeeDbContext>
{
    public EmployeeDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__EmployeeDb")
            ?? "Host=localhost;Port=5432;Database=employeeapp;Username=postgres";

        var options = new DbContextOptionsBuilder<EmployeeDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new EmployeeDbContext(options);
    }
}
