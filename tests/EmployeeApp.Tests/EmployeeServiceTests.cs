using EmployeeApp.Core.Exceptions;
using EmployeeApp.Core.Models;
using EmployeeApp.Core.Services;

namespace EmployeeApp.Tests;

/// <summary>
/// 4.4 xUnit creates a NEW instance of this class for every test, so each test gets a fresh service
/// from the constructor and tests can't leak state into each other.
/// Every test follows Arrange / Act / Assert.
/// </summary>
public class EmployeeServiceTests
{
    private readonly EmployeeService _service = TestData.CreateService();

    // ---------- Hire ----------

    [Fact]
    public void Hire_LowercaseDepartmentCode_StoresNormalisedCode()
    {
        // Arrange
        var hire = new FullTimeEmployee { Id = 6, Name = "Divya", DepartmentCode = " eng ", MonthlySalary = 85000m };

        // Act
        _service.Hire(hire);

        // Assert
        Assert.Equal("ENG", _service.GetEmployee(6).DepartmentCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Hire_BlankName_ThrowsValidationException(string name)
    {
        var hire = new FullTimeEmployee { Id = 6, Name = name, DepartmentCode = "ENG", MonthlySalary = 50000m };

        Assert.Throws<ValidationException>(() => _service.Hire(hire));
    }

    [Fact]
    public void Hire_UnknownDepartment_ThrowsDepartmentNotFound()
    {
        var hire = new FullTimeEmployee { Id = 6, Name = "Divya", DepartmentCode = "MARKETING", MonthlySalary = 50000m };

        var ex = Assert.Throws<DepartmentNotFoundException>(() => _service.Hire(hire));

        Assert.Equal("MARKETING", ex.Code);
    }

    [Fact]
    public void Hire_InternAboveSalaryCap_ThrowsValidationException()
    {
        var hire = new Intern { Id = 6, Name = "Sam", DepartmentCode = "ENG", MonthlySalary = EmployeeService.MaxInternSalary + 1 };

        Assert.Throws<ValidationException>(() => _service.Hire(hire));
    }

    [Fact]
    public void Hire_ManagerIdThatDoesNotExist_ThrowsEmployeeNotFound()
    {
        var hire = new FullTimeEmployee { Id = 6, Name = "Divya", DepartmentCode = "ENG", MonthlySalary = 50000m, ManagerId = 42 };

        Assert.Throws<EmployeeNotFoundException>(() => _service.Hire(hire));
    }

    // ---------- Update (Week 2: PUT) ----------

    [Fact]
    public void Update_ExistingEmployee_ReplacesItIncludingRole()
    {
        var replacement = new Manager { Id = 2, Name = "Priya", DepartmentCode = "sales", MonthlySalary = 120000m };

        _service.Update(replacement);

        Employee stored = _service.GetEmployee(2);
        Assert.IsType<Manager>(stored);                 // was FullTimeEmployee
        Assert.Equal("SALES", stored.DepartmentCode);   // normalised, like Hire
        Assert.Equal(120000m, stored.MonthlySalary);
    }

    [Fact]
    public void Update_UnknownId_ThrowsEmployeeNotFound()
    {
        var ghost = new FullTimeEmployee { Id = 99, Name = "Ghost", DepartmentCode = "ENG", MonthlySalary = 1000m };

        Assert.Throws<EmployeeNotFoundException>(() => _service.Update(ghost));
    }

    [Fact]
    public void Update_OwnManager_ThrowsValidationException()
    {
        var selfManaged = new FullTimeEmployee { Id = 2, Name = "Priya", DepartmentCode = "ENG", MonthlySalary = 90000m, ManagerId = 2 };

        Assert.Throws<ValidationException>(() => _service.Update(selfManaged));
    }

    [Fact]
    public void Update_InvalidData_LeavesOriginalUntouched()
    {
        var badDept = new FullTimeEmployee { Id = 2, Name = "Priya", DepartmentCode = "NOPE", MonthlySalary = 1m };

        Assert.Throws<DepartmentNotFoundException>(() => _service.Update(badDept));

        Assert.Equal(90000m, _service.GetEmployee(2).MonthlySalary);   // validated BEFORE the old one was removed
    }

    // ---------- Fire (Week 2: DELETE) ----------

    [Fact]
    public void Fire_EmployeeWithNoReports_RemovesThem()
    {
        _service.Fire(2);

        Assert.Throws<EmployeeNotFoundException>(() => _service.GetEmployee(2));
        Assert.Equal(4, _service.GetEmployees().Count);
    }

    [Fact]
    public void Fire_ManagerWithReports_ThrowsAndKeepsThem()
    {
        Assert.Throws<InvalidOperationException>(() => _service.Fire(1));   // Priya and Arun report to Ravi

        Assert.Equal("Ravi", _service.GetEmployee(1).Name);
    }

    [Fact]
    public void Fire_UnknownId_ThrowsEmployeeNotFound()
    {
        Assert.Throws<EmployeeNotFoundException>(() => _service.Fire(99));
    }

    // ---------- Not found ----------

    [Fact]
    public void GetEmployee_UnknownId_ThrowsWithThatId()
    {
        var ex = Assert.Throws<EmployeeNotFoundException>(() => _service.GetEmployee(99));

        Assert.Equal(99, ex.EmployeeId);
    }

    // ---------- Raise ----------

    [Theory]
    [InlineData(0)]        // must be MORE than 0
    [InlineData(-5)]
    [InlineData(50.01)]    // just over the cap
    [InlineData(80)]
    public void GiveRaise_PercentOutOfRange_ThrowsValidationException(decimal percent)
    {
        Assert.Throws<ValidationException>(() => _service.GiveRaise(2, percent));
    }

    [Theory]
    [InlineData(0.01, 90009.00)]    // smallest allowed
    [InlineData(10, 99000.00)]
    [InlineData(50, 135000.00)]     // exactly the cap is allowed
    public void GiveRaise_PercentInRange_ReturnsAndStoresNewSalary(decimal percent, decimal expected)
    {
        decimal result = _service.GiveRaise(2, percent);   // Priya earns 90,000

        Assert.Equal(expected, result);
        Assert.Equal(expected, _service.GetEmployee(2).MonthlySalary);
    }

    // ---------- 3.3 Event ----------

    [Fact]
    public void GiveRaise_Valid_RaisesSalaryChangedWithOldAndNewSalary()
    {
        // Arrange: a lambda subscriber that just records what it received
        SalaryChangedEventArgs? received = null;
        _service.SalaryChanged += (_, e) => received = e;

        // Act
        _service.GiveRaise(2, 10);

        // Assert
        Assert.NotNull(received);
        Assert.Equal("Priya", received.Employee.Name);
        Assert.Equal(90000m, received.OldSalary);
        Assert.Equal(99000m, received.NewSalary);
    }

    [Fact]
    public void GiveRaise_Invalid_DoesNotRaiseSalaryChanged()
    {
        int calls = 0;
        _service.SalaryChanged += (_, _) => calls++;

        Assert.Throws<ValidationException>(() => _service.GiveRaise(2, 80));

        Assert.Equal(0, calls);
    }

    // ---------- 2.3 Polymorphism: each type's own CalculateBonus runs ----------

    [Theory]
    [InlineData(1, 37000)]   // Manager Ravi: 160,000 × 20% + 5,000
    [InlineData(2, 9000)]    // Full-time Priya: 90,000 × 10%
    [InlineData(3, 0)]       // Intern Arun: no bonus
    public void CalculateBonus_DependsOnEmployeeType(int id, decimal expectedBonus)
    {
        Employee employee = _service.GetEmployee(id);   // static type is Employee; runtime type decides

        Assert.Equal(expectedBonus, employee.CalculateBonus());
    }

    // ---------- 3.4 LINQ queries ----------

    [Fact]
    public void GetByDepartment_IsCaseInsensitive_AndReturnsOnlyThatDepartment()
    {
        var eng = _service.GetByDepartment("eng");

        Assert.Equal(["Ravi", "Priya", "Arun"], eng.Select(e => e.Name));
    }

    [Fact]
    public void SearchByName_MatchesAnyPartIgnoringCase()
    {
        var found = _service.SearchByName("AR");

        Assert.Equal([3, 5], found.Select(e => e.Id));   // Arun, Kumar
    }

    [Fact]
    public void SearchByName_BlankTerm_ReturnsEmpty()
    {
        Assert.Empty(_service.SearchByName("  "));
    }

    [Fact]
    public void Find_UsesTheGivenFilter()
    {
        var managers = _service.Find(e => e is Manager);

        Assert.Equal(["Ravi", "Meena"], managers.Select(e => e.Name));
    }

    [Fact]
    public void NextEmployeeId_IsHighestIdPlusOne()
    {
        Assert.Equal(6, _service.NextEmployeeId());
    }

    [Fact]
    public void HeadcountByDepartment_CountsStaff_AndIncludesEmptyDepartments()
    {
        var counts = _service.HeadcountByDepartment();

        Assert.Equal(3, counts["ENG"]);
        Assert.Equal(1, counts["HR"]);
        Assert.Equal(1, counts["SALES"]);
        Assert.Equal(0, counts["OPS"]);
    }

    [Fact]
    public void BuildPayrollReport_ShowsDepartmentTotalsAndGrandTotal()
    {
        string report = _service.BuildPayrollReport();

        // Expected numbers are formatted with the current culture (en-IN prints 2,73,000.00; en-US 273,000.00).
        Assert.Contains($"ENG   Engineering     3 staff {273000m,14:N2}", report);
        Assert.EndsWith($"TOTAL{453000m,39:N2}", report);
    }
}
