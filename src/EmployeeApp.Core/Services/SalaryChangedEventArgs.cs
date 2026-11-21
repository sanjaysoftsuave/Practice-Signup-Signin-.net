using EmployeeApp.Core.Models;

namespace EmployeeApp.Core.Services;

/// <summary>
/// 3.3 Event data for <see cref="EmployeeService.SalaryChanged"/>. The .NET convention is a class
/// deriving from <see cref="EventArgs"/>, used with <c>EventHandler&lt;T&gt;</c>.
/// (A record can't be used here: records may only inherit from other records.)
/// </summary>
public sealed class SalaryChangedEventArgs(Employee employee, decimal oldSalary, decimal newSalary) : EventArgs
{
    public Employee Employee { get; } = employee;
    public decimal OldSalary { get; } = oldSalary;
    public decimal NewSalary { get; } = newSalary;
}
