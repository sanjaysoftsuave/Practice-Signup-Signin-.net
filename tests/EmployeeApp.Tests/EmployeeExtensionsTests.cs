using EmployeeApp.Core.Models;
using EmployeeApp.Core.Services;

namespace EmployeeApp.Tests;

/// <summary>3.5 Extension methods are plain static methods, so they're tested on a plain list.</summary>
public class EmployeeExtensionsTests
{
    private readonly List<Employee> _staff =
    [
        new Manager { Id = 1, Name = "Ravi", DepartmentCode = "ENG", MonthlySalary = 160000m },
        new FullTimeEmployee { Id = 2, Name = "Priya", DepartmentCode = "ENG", MonthlySalary = 90000m, Location = new Remote() },
        new Manager { Id = 4, Name = "Meena", DepartmentCode = "HR", MonthlySalary = 120000m },
    ];

    [Fact]
    public void TotalMonthlyPay_AddsSalaryAndLocationAllowance()
    {
        decimal total = _staff.TotalMonthlyPay();

        Assert.Equal(160000m + 92000m + 120000m, total);   // Priya: 90,000 + 2,000 remote allowance
    }

    [Fact]
    public void TotalMonthlyPay_EmptySequence_IsZero()
    {
        Assert.Equal(0m, new List<Employee>().TotalMonthlyPay());
    }

    [Fact]
    public void InDepartment_MatchesCodeIgnoringCase()
    {
        var hr = _staff.InDepartment("hr").ToList();

        var only = Assert.Single(hr);
        Assert.Equal("Meena", only.Name);
    }

    [Fact]
    public void InDepartment_IsDeferred_SeesItemsAddedAfterTheCall()
    {
        // Arrange: build the query but don't run it yet
        var engQuery = _staff.InDepartment("ENG");

        // Act: change the source AFTER creating the query
        _staff.Add(new Intern { Id = 3, Name = "Arun", DepartmentCode = "ENG", MonthlySalary = 20000m });

        // Assert: 3.4 deferred execution, the query runs now and sees Arun
        Assert.Equal(3, engQuery.Count());
    }
}
