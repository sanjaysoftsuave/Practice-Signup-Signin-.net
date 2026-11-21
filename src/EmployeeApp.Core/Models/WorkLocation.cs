namespace EmployeeApp.Core.Models;

/// <summary>
/// 2.6 Composition over inheritance: an <see cref="Employee"/> HAS a work location,
/// instead of RemoteEmployee / OfficeEmployee subclasses. It can be swapped at runtime.
/// </summary>
public interface IWorkLocation
{
    string Name { get; }
    decimal MonthlyAllowance { get; }
}

public sealed class Office : IWorkLocation
{
    public string Name => "Office";
    public decimal MonthlyAllowance => 0m;
}

public sealed class Remote : IWorkLocation
{
    public string Name => "Remote";
    public decimal MonthlyAllowance => 2000m;   // internet allowance
}

public sealed class Hybrid : IWorkLocation
{
    public string Name => "Hybrid";
    public decimal MonthlyAllowance => 1000m;
}
