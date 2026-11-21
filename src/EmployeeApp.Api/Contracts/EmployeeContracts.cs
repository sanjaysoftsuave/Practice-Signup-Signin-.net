using System.ComponentModel.DataAnnotations;

namespace EmployeeApp.Api.Contracts;

// 6.4 DTOs (Data Transfer Objects): the JSON shapes the API accepts and returns.
// They are NOT the Core Employee class. Why separate?
//   - The client must not set Id or JoinedOn: the server decides those.
//   - Employee is abstract (Manager/Intern/FullTime). JSON has no classes, so "role" is just a field.
//   - We can change Employee's internals later without breaking every client (the DTO is the contract).
// Java equivalent: request/response DTO classes next to a Spring @RestController.

/// <summary>Kind of employee. Sent and returned as a string: "FullTime", "Manager" or "Intern".</summary>
public enum EmployeeRole
{
    FullTime,
    Manager,
    Intern
}

/// <summary>Where the employee works: "Office", "Remote" or "Hybrid".</summary>
public enum WorkLocationType
{
    Office,
    Remote,
    Hybrid
}

/// <summary>
/// Body for POST (create) and PUT (replace). There's no Id here: the URL carries it for PUT,
/// and the server generates it for POST.
/// 6.4 The attributes are checked automatically by [ApiController] BEFORE the action runs;
/// a failure returns 400 with the list of errors (Day 8 goes deeper into validation).
/// </summary>
public sealed class EmployeeRequest
{
    /// <example>Divya</example>
    [Required, StringLength(100)]
    public string Name { get; init; } = "";

    /// <summary>Department code, e.g. ENG, HR or SALES (case doesn't matter).</summary>
    /// <example>ENG</example>
    [Required, StringLength(10)]
    public string DepartmentCode { get; init; } = "";

    /// <example>85000</example>
    [Range(typeof(decimal), "0", "10000000")]
    public decimal MonthlySalary { get; init; }

    public EmployeeRole Role { get; init; } = EmployeeRole.FullTime;

    /// <example>divya@example.com</example>
    [EmailAddress]
    public string? Email { get; init; }

    /// <summary>Id of this employee's manager. Leave out (null) for no manager.</summary>
    /// <example>1</example>
    public int? ManagerId { get; init; }

    public WorkLocationType Location { get; init; } = WorkLocationType.Office;
}

/// <summary>
/// What the API returns for one employee. 2.5 A record: read-only data, no behaviour.
/// MonthlyPay and Bonus are calculated by the Core rules; the client can't send them.
/// </summary>
public sealed record EmployeeResponse(
    int Id,
    string Name,
    EmployeeRole Role,
    string DepartmentCode,
    string? Email,
    int? ManagerId,
    WorkLocationType Location,
    decimal MonthlySalary,
    decimal MonthlyPay,
    decimal Bonus,
    DateTime JoinedOn);
