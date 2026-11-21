using EmployeeApp.Core.Models;

namespace EmployeeApp.Core.Services;

/// <summary>
/// The starting data shared by the console app and the Web API, so both show the same company.
/// (Week 1 kept this inside the CLI's Program.cs; it moved here when a second app needed it.)
/// </summary>
public static class SampleData
{
    public static void Seed(EmployeeService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        service.AddDepartment(new Department { Id = 1, Code = "ENG", Name = "Engineering" });
        service.AddDepartment(new Department { Id = 2, Code = "HR", Name = "Human Res." });
        service.AddDepartment(new Department { Id = 3, Code = "SALES", Name = "Sales" });

        service.Hire(new Manager { Id = 1, Name = "Ravi", DepartmentCode = "ENG", MonthlySalary = 160000m });
        service.Hire(new FullTimeEmployee
        {
            Id = 2, Name = "Priya", DepartmentCode = "ENG", MonthlySalary = 90000m,
            ManagerId = 1, Location = new Remote(), Address = new Address("Chennai", "600001")
        });
        service.Hire(new Intern { Id = 3, Name = "Arun", DepartmentCode = "ENG", MonthlySalary = 20000m, ManagerId = 1, Location = new Hybrid() });
        service.Hire(new Manager { Id = 4, Name = "Meena", DepartmentCode = "HR", MonthlySalary = 120000m });
        service.Hire(new FullTimeEmployee { Id = 5, Name = "Kumar", DepartmentCode = "SALES", MonthlySalary = 60000m, ManagerId = 4 });
    }
}
