using System.Text;
using EmployeeApp.Core.Exceptions;
using EmployeeApp.Core.Models;
using EmployeeApp.Core.Repositories;

namespace EmployeeApp.Core.Services;

/// <summary>
/// Business rules for employees and departments.
/// 2.1 Primary constructor + 2.6 Dependency Inversion: repositories are injected as interfaces.
/// 2.6 Single Responsibility: rules live here; storage lives in the repository; I/O lives in the CLI.
/// 3.4 The Day 1–2 foreach loops have been rewritten as LINQ queries.
/// </summary>
public class EmployeeService(IRepository<Employee> employees, IRepository<Department> departments)
{
    public const decimal MaxRaisePercent = 50m;    // 1.3 const: compile-time constant
    public const decimal MaxInternSalary = 50000m;

    /// <summary>
    /// 3.3 Event: "tell me when a salary changes". Subscribers use += / -=; only this class can raise it.
    /// The service doesn't know who is listening (the CLI prints an audit line; tests just record it).
    /// </summary>
    public event EventHandler<SalaryChangedEventArgs>? SalaryChanged;

    // ---------- Departments ----------

    public IReadOnlyList<Department> GetDepartments() => departments.GetAll();

    public void AddDepartment(Department department) => departments.Add(department);

    /// <summary>
    /// Case-insensitive lookup by code. Throws <see cref="DepartmentNotFoundException"/>.
    /// 3.4 FirstOrDefault replaces the search loop; 1.6 <c>?? throw</c> handles "not found".
    /// </summary>
    public Department GetDepartment(string code) =>
        departments.GetAll().FirstOrDefault(d => Codes.Match(d.Code, code))
            ?? throw new DepartmentNotFoundException(code);

    // ---------- Employees ----------

    public IReadOnlyList<Employee> GetEmployees() => employees.GetAll();

    /// <summary>Throws <see cref="EmployeeNotFoundException"/> instead of returning null.</summary>
    public Employee GetEmployee(int id) =>
        employees.GetById(id) ?? throw new EmployeeNotFoundException(id);   // 1.6 ?? with throw

    /// <summary>
    /// 3.4 Max() throws on an empty sequence, so DefaultIfEmpty(0) makes "no employees yet" return 1.
    /// </summary>
    public int NextEmployeeId() =>
        employees.GetAll().Select(e => e.Id).DefaultIfEmpty(0).Max() + 1;

    /// <summary>Validates and stores a new employee. The department code is normalised to the stored one.</summary>
    public void Hire(Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);
        Validate(employee);
        employees.Add(employee);
    }

    /// <summary>
    /// Week 2 (PUT): replaces an existing employee with a new version that has the same Id.
    /// A full replacement, so the role can change too (Full-time → Manager is a different class).
    /// Throws <see cref="EmployeeNotFoundException"/> when the Id doesn't exist.
    /// Validates BEFORE saving, so a bad update leaves the original untouched.
    /// </summary>
    public void Update(Employee replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        GetEmployee(replacement.Id);   // must already exist

        if (replacement.ManagerId == replacement.Id)
            throw new ValidationException("An employee can't be their own manager.");
        Validate(replacement);

        employees.Update(replacement);
    }

    /// <summary>
    /// Week 2 (DELETE): removes an employee. Throws <see cref="EmployeeNotFoundException"/> when the Id
    /// doesn't exist, and <see cref="InvalidOperationException"/> when other employees still report to them
    /// (deleting them would leave those employees pointing at a manager who no longer exists).
    /// </summary>
    public void Fire(int id)
    {
        GetEmployee(id);   // must exist

        if (employees.GetAll().Any(e => e.ManagerId == id))   // 3.4 Any stops at the first match
            throw new InvalidOperationException($"Employee #{id} still manages other employees. Reassign them first.");

        employees.Remove(id);
    }

    /// <summary>The rules shared by <see cref="Hire"/> and <see cref="Update"/>.</summary>
    private void Validate(Employee employee)
    {
        if (string.IsNullOrWhiteSpace(employee.Name))   // 1.7
            throw new ValidationException("Name is required.");

        // 3.6 Type + property + relational pattern in one test: "an Intern whose salary is above the cap".
        if (employee is Intern { MonthlySalary: > MaxInternSalary })
            throw new ValidationException($"An intern's salary can't be more than {MaxInternSalary:N0}.");

        employee.DepartmentCode = GetDepartment(employee.DepartmentCode).Code;   // "eng" → "ENG"

        // 3.6 Property pattern: "ManagerId has a value", and unwrap the int? into managerId at the same time.
        if (employee is { ManagerId: int managerId })
            GetEmployee(managerId);                                               // must exist
    }

    /// <summary>
    /// 3.3 Takes the filter rule as a <see cref="Func{T, TResult}"/>: the caller decides WHAT to match,
    /// this method decides WHERE to look. 3.4 ToList() runs the query once and returns a snapshot.
    /// </summary>
    public List<Employee> Find(Func<Employee, bool> filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return employees.GetAll().Where(filter).ToList();
    }

    /// <summary>3.5 Uses the <see cref="EmployeeExtensions.InDepartment"/> extension method.</summary>
    public List<Employee> GetByDepartment(string code)
    {
        var dept = GetDepartment(code);   // throws if the department doesn't exist
        return employees.GetAll().InDepartment(dept.Code).ToList();
    }

    /// <summary>
    /// 3.3 Closure: the lambda passed to <see cref="Find"/> captures the local variable <c>trimmed</c>.
    /// </summary>
    public List<Employee> SearchByName(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return [];

        string trimmed = term.Trim();
        return Find(e => e.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Raises salary by a percentage, raises <see cref="SalaryChanged"/>, and returns the new salary.</summary>
    public decimal GiveRaise(int id, decimal percent)
    {
        if (percent <= 0 || percent > MaxRaisePercent)
            throw new ValidationException($"Raise must be more than 0% and at most {MaxRaisePercent}%.");

        var employee = GetEmployee(id);   // reference type: we change the stored object itself (1.5)
        decimal oldSalary = employee.MonthlySalary;
        employee.MonthlySalary += Math.Round(employee.MonthlySalary * percent / 100m, 2);
        employees.Update(employee);   // Week 2: a database must be told about the change

        // 3.3 ?.Invoke: an event with no subscribers is null, so this is a safe "raise if anyone listens".
        SalaryChanged?.Invoke(this, new SalaryChangedEventArgs(employee, oldSalary, employee.MonthlySalary));
        return employee.MonthlySalary;
    }

    public void Transfer(int id, string departmentCode)
    {
        var employee = GetEmployee(id);
        employee.DepartmentCode = GetDepartment(departmentCode).Code;
        employees.Update(employee);
    }

    /// <summary>2.6 Composition: change where someone works without changing their type.</summary>
    public void ChangeLocation(int id, IWorkLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        var employee = GetEmployee(id);
        employee.Location = location;
        employees.Update(employee);
    }

    /// <summary>
    /// Replaces every employee with a loaded set (used by "Load from file").
    /// Everything is validated BEFORE anything is removed, so a bad file can't leave the app half-loaded.
    /// </summary>
    public void ReplaceAllEmployees(IEnumerable<Employee> loaded)
    {
        ArgumentNullException.ThrowIfNull(loaded);
        List<Employee> incoming = loaded.ToList();   // 3.4 materialise once: it's enumerated three times below

        if (incoming.Select(e => e.Id).Distinct().Count() != incoming.Count)
            throw new ValidationException("The file contains duplicate employee IDs.");

        foreach (var e in incoming)
            e.DepartmentCode = GetDepartment(e.DepartmentCode).Code;   // throws on an unknown department

        foreach (var e in employees.GetAll())   // GetAll returns a copy, so removing while looping is safe
            employees.Remove(e.Id);
        foreach (var e in incoming)
            employees.Add(e);
    }

    // ---------- Reporting ----------

    public EmployeeSummary Summarize(Employee e)
    {
        // 1.6 int? pattern + ?. + ?? : "none" when there's no manager or the manager is missing
        string managerName = (e.ManagerId is int mid ? employees.GetById(mid)?.Name : null) ?? "none";

        return new EmployeeSummary(
            e.Id, e.Name, e.Role, e.DepartmentCode, e.Location.Name,
            e.MonthlyPay(),
            e.CalculateBonus(),   // 2.3 virtual dispatch: Manager / Intern versions run here
            SalaryBands.For(e.MonthlySalary),
            managerName);
    }

    /// <summary>
    /// 3.1 Dictionary: department code → headcount. 3.4 GroupBy + ToDictionary build it in one pass;
    /// departments with nobody in them still appear, with 0.
    /// </summary>
    public Dictionary<string, int> HeadcountByDepartment()
    {
        Dictionary<string, int> staffPerCode = employees.GetAll()
            .GroupBy(e => e.DepartmentCode)
            .ToDictionary(g => g.Key, g => g.Count());

        return departments.GetAll()
            .ToDictionary(d => d.Code, d => staffPerCode.GetValueOrDefault(d.Code));   // 0 if missing
    }

    /// <summary>
    /// 3.4 GroupBy + ToDictionary + Sum (via the 3.5 TotalMonthlyPay extension) replace the totals loop.
    /// 1.7 StringBuilder: appending in a loop.
    /// </summary>
    public string BuildPayrollReport()
    {
        // GetAll() already returns a List, so 'staff' is safe to enumerate twice (no repeated query work).
        IReadOnlyList<Employee> staff = employees.GetAll();

        Dictionary<string, decimal> payByCode = staff
            .GroupBy(e => e.DepartmentCode)
            .ToDictionary(g => g.Key, g => g.TotalMonthlyPay());
        decimal grandTotal = staff.TotalMonthlyPay();

        var headcount = HeadcountByDepartment();
        var sb = new StringBuilder();
        sb.AppendLine("Monthly payroll by department");
        sb.AppendLine(new string('-', 44));
        foreach (var dept in departments.GetAll())
        {
            decimal total = payByCode.GetValueOrDefault(dept.Code);
            sb.AppendLine($"{dept.Code,-6}{dept.Name,-14}{headcount[dept.Code],3} staff {total,14:N2}");
        }
        sb.AppendLine(new string('-', 44));
        sb.Append($"{"TOTAL",-29}{grandTotal,15:N2}");
        return sb.ToString();
    }
}
