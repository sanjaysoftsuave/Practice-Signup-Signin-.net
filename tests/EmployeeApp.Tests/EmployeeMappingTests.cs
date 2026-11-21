using EmployeeApp.Api.Contracts;
using EmployeeApp.Core.Exceptions;
using EmployeeApp.Core.Models;

namespace EmployeeApp.Tests;

/// <summary>
/// Week 2 Day 6: the DTO ↔ model mapping is plain C# with no HTTP in it, so it's tested
/// like any other logic: no web server needed.
/// </summary>
public class EmployeeMappingTests
{
    private static EmployeeRequest Request(EmployeeRole role = EmployeeRole.FullTime,
                                           WorkLocationType location = WorkLocationType.Office,
                                           decimal salary = 50000m) => new()
    {
        Name = "  Divya  ",
        DepartmentCode = "ENG",
        MonthlySalary = salary,
        Role = role,
        Location = location,
        Email = "divya@example.com",
        ManagerId = 1
    };

    [Theory]
    [InlineData(EmployeeRole.FullTime, typeof(FullTimeEmployee))]
    [InlineData(EmployeeRole.Manager, typeof(Manager))]
    [InlineData(EmployeeRole.Intern, typeof(Intern))]
    public void ToEmployee_Role_CreatesMatchingSubclass(EmployeeRole role, Type expected)
    {
        Employee employee = Request(role).ToEmployee(id: 6, joinedOn: new DateTime(2026, 1, 5));

        Assert.IsType(expected, employee);
    }

    [Fact]
    public void ToEmployee_CopiesFields_TrimsName_UsesServerIdAndDate()
    {
        var joined = new DateTime(2026, 1, 5);

        Employee employee = Request(location: WorkLocationType.Remote).ToEmployee(id: 6, joinedOn: joined);

        Assert.Equal(6, employee.Id);
        Assert.Equal("Divya", employee.Name);
        Assert.Equal("ENG", employee.DepartmentCode);
        Assert.Equal(50000m, employee.MonthlySalary);
        Assert.Equal("divya@example.com", employee.Email);
        Assert.Equal(1, employee.ManagerId);
        Assert.IsType<Remote>(employee.Location);
        Assert.Equal(joined, employee.JoinedOn);
    }

    [Fact]
    public void ToEmployee_NegativeSalary_ThrowsValidationException()
    {
        Assert.Throws<ValidationException>(() => Request(salary: -1m).ToEmployee(6, DateTime.Today));
    }

    [Fact]
    public void ToResponse_Manager_IncludesCalculatedPayAndBonus()
    {
        var manager = new Manager
        {
            Id = 1, Name = "Ravi", DepartmentCode = "ENG", MonthlySalary = 100000m, Location = new Hybrid()
        };

        EmployeeResponse response = manager.ToResponse();

        Assert.Equal(EmployeeRole.Manager, response.Role);
        Assert.Equal(WorkLocationType.Hybrid, response.Location);
        Assert.Equal(101000m, response.MonthlyPay);   // salary + 1,000 hybrid allowance
        Assert.Equal(25000m, response.Bonus);         // 20% + 5,000 (Manager override)
    }

    [Fact]
    public void RoundTrip_RequestToEmployeeToResponse_KeepsRoleAndLocation()
    {
        EmployeeResponse response = Request(EmployeeRole.Intern, WorkLocationType.Hybrid)
            .ToEmployee(6, DateTime.Today)
            .ToResponse();

        Assert.Equal(EmployeeRole.Intern, response.Role);
        Assert.Equal(WorkLocationType.Hybrid, response.Location);
        Assert.Equal(0m, response.Bonus);   // interns get no bonus
    }
}
