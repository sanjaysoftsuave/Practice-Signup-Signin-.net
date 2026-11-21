using System.Text.Json;
using EmployeeApp.Core.Models;

namespace EmployeeApp.Core.Persistence;

/// <summary>
/// 2.5 Record used as a flat storage DTO: one JSON object per employee.
/// Why not serialize <see cref="Employee"/> directly? It's abstract, so System.Text.Json can't
/// create one when reading back, and <see cref="IWorkLocation"/> is an interface. The DTO stores
/// the concrete type and the location as plain strings instead.
/// </summary>
public record EmployeeRecord(
    int Id,
    string Type,
    string Name,
    string DepartmentCode,
    decimal MonthlySalary,
    string Location,
    DateTime JoinedOn,
    int? ManagerId = null,      // optional fields have defaults, so they may be left out of the JSON
    string? Email = null,
    Address? Address = null)
{
    /// <summary>
    /// 3.6 Switch expression with type patterns: the runtime type picks the arm.
    /// The <c>_</c> arm catches a future subclass we forgot to map, instead of saving bad data.
    /// </summary>
    public static EmployeeRecord From(Employee e) => new(
        e.Id,
        Type: e switch
        {
            Manager => "Manager",
            Intern => "Intern",
            FullTimeEmployee => "FullTime",
            _ => throw new NotSupportedException($"Can't save employee type {e.GetType().Name}.")
        },
        e.Name,
        e.DepartmentCode,
        e.MonthlySalary,
        e.Location.Name,
        e.JoinedOn,
        e.ManagerId,
        e.Email,
        e.Address);

    /// <summary>3.6 Switch expressions on strings: the reverse mapping, DTO → real object.</summary>
    public Employee ToEmployee()
    {
        Employee employee = Type switch
        {
            "Manager" => new Manager { Id = Id, Name = Name, DepartmentCode = DepartmentCode, JoinedOn = JoinedOn },
            "Intern" => new Intern { Id = Id, Name = Name, DepartmentCode = DepartmentCode, JoinedOn = JoinedOn },
            "FullTime" => new FullTimeEmployee { Id = Id, Name = Name, DepartmentCode = DepartmentCode, JoinedOn = JoinedOn },
            _ => throw new JsonException($"Unknown employee type '{Type}' in the file.")
        };

        // These have public setters, so they're set once here instead of in all three arms above.
        employee.MonthlySalary = MonthlySalary;
        employee.ManagerId = ManagerId;
        employee.Email = Email;
        employee.Address = Address;
        employee.Location = Location switch
        {
            "Remote" => new Remote(),
            "Hybrid" => new Hybrid(),
            _ => new Office()
        };
        return employee;
    }
}
