using EmployeeApp.Core.Exceptions;

namespace EmployeeApp.Core.Models;

/// <summary>
/// 2.4 Abstract class: shared state (Id, Name, Salary…) and shared code for every kind of employee.
/// It's a class, not a record, because an employee has identity (2.5): Ravi stays Ravi when his salary changes.
/// </summary>
public abstract class Employee : IEntity
{
    private decimal _monthlySalary;   // 2.1 private backing field, _camelCase

    public int Id { get; init; }                         // 2.1 init: set once at creation
    public required string Name { get; init; }           // 2.1 required: fixes CS8618 (1.6)
    public required string DepartmentCode { get; set; }  // mutable: employees can be transferred
    public string? Email { get; set; }                   // 1.6 nullable reference type: optional
    public int? ManagerId { get; set; }                  // 1.6 Nullable<int>: the CEO has no manager
    public Address? Address { get; set; }
    public DateTime JoinedOn { get; init; } = DateTime.Today;
    public IWorkLocation Location { get; set; } = new Office();   // 2.6 composition

    /// <summary>1.3 decimal for money. 2.1 a full property with validation in <c>set</c>.</summary>
    public decimal MonthlySalary
    {
        get => _monthlySalary;
        set
        {
            if (value < 0)
                throw new ValidationException("Salary can't be negative.");
            _monthlySalary = value;
        }
    }

    /// <summary>2.4 Abstract: every concrete employee type must say what it is.</summary>
    public abstract string Role { get; }

    /// <summary>2.3 virtual: children MAY override (C# methods are non-virtual by default).</summary>
    public virtual decimal CalculateBonus() => MonthlySalary * 0.10m;

    /// <summary>Not virtual: the same rule for everyone. Uses the composed location.</summary>
    public decimal MonthlyPay() => MonthlySalary + Location.MonthlyAllowance;

    public override string ToString() => $"#{Id} {Name} ({Role}, {DepartmentCode}, {Location.Name})";
}
