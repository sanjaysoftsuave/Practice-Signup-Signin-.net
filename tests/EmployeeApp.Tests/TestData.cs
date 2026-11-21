using EmployeeApp.Core.Models;
using EmployeeApp.Core.Repositories;
using EmployeeApp.Core.Services;

namespace EmployeeApp.Tests;

/// <summary>
/// 4.4 Shared "Arrange" helpers. The real InMemoryRepository is already a perfect test double:
/// fast, no disk, no network, so no mocking library is needed.
/// </summary>
internal static class TestData
{
    /// <summary>Same data as the CLI seed: ENG (Ravi, Priya, Arun), HR (Meena), SALES (Kumar), plus an empty OPS.</summary>
    public static EmployeeService CreateService()
    {
        var service = new EmployeeService(new InMemoryRepository<Employee>(), new InMemoryRepository<Department>());

        service.AddDepartment(new Department { Id = 1, Code = "ENG", Name = "Engineering" });
        service.AddDepartment(new Department { Id = 2, Code = "HR", Name = "Human Res." });
        service.AddDepartment(new Department { Id = 3, Code = "SALES", Name = "Sales" });
        service.AddDepartment(new Department { Id = 4, Code = "OPS", Name = "Operations" });   // nobody works here

        service.Hire(new Manager { Id = 1, Name = "Ravi", DepartmentCode = "ENG", MonthlySalary = 160000m });
        service.Hire(new FullTimeEmployee
        {
            Id = 2, Name = "Priya", DepartmentCode = "ENG", MonthlySalary = 90000m,
            ManagerId = 1, Location = new Remote(), Address = new Address("Chennai", "600001")
        });
        service.Hire(new Intern { Id = 3, Name = "Arun", DepartmentCode = "ENG", MonthlySalary = 20000m, ManagerId = 1, Location = new Hybrid() });
        service.Hire(new Manager { Id = 4, Name = "Meena", DepartmentCode = "HR", MonthlySalary = 120000m });
        service.Hire(new FullTimeEmployee { Id = 5, Name = "Kumar", DepartmentCode = "SALES", MonthlySalary = 60000m, ManagerId = 4 });

        return service;
    }
}
