namespace EmployeeApp.Core.Models;

/// <summary>2.5 Record used as a read-only DTO for display: data only, no behaviour, no identity.</summary>
public record EmployeeSummary(
    int Id,
    string Name,
    string Role,
    string Department,
    string Location,
    decimal MonthlyPay,
    decimal Bonus,
    string Band,
    string ManagerName);
