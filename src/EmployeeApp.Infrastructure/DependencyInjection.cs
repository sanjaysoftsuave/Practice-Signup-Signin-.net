using EmployeeApp.Core.Models;
using EmployeeApp.Core.Repositories;
using EmployeeApp.Infrastructure.Data;
using EmployeeApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeApp.Infrastructure;

/// <summary>
/// 7.1 One call registers the whole data layer, so the API's Program.cs doesn't need to know
/// about EF Core, Npgsql or EfRepository: <c>builder.Services.AddEmployeeDatabase(connectionString)</c>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddEmployeeDatabase(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<EmployeeDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());   // MonthlySalary → monthly_salary

        // Scoped = one per HTTP request, matching the DbContext they use.
        services.AddScoped<IRepository<Employee>, EfRepository<Employee>>();
        services.AddScoped<IRepository<Department>, EfRepository<Department>>();
        services.AddScoped<IRepository<AppUser>, EfRepository<AppUser>>();
        return services;
    }
}
