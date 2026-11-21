namespace EmployeeApp.Core.Models;

/// <summary>Uses the default 10% bonus from <see cref="Employee"/>.</summary>
public class FullTimeEmployee : Employee
{
    public override string Role => "Full-time";
}

/// <summary>2.3 override: runtime polymorphism. Called through an <c>Employee</c> variable, this version still runs.</summary>
public class Manager : Employee
{
    public override string Role => "Manager";

    public override decimal CalculateBonus() => MonthlySalary * 0.20m + 5000m;
}

/// <summary>
/// 2.3 sealed: nothing can inherit from Intern (Java: final class).
/// 2.6 Liskov: interns get no bonus, so return a valid 0, never throw NotSupportedException.
/// </summary>
public sealed class Intern : Employee
{
    public override string Role => "Intern";

    public override decimal CalculateBonus() => 0m;
}
