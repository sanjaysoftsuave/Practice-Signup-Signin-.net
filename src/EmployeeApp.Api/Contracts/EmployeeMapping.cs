using EmployeeApp.Core.Exceptions;
using EmployeeApp.Core.Models;

namespace EmployeeApp.Api.Contracts;

/// <summary>
/// 6.4 Converts between the DTOs (JSON shapes) and the Core model.
/// 3.5 Extension methods, so the call reads <c>request.ToEmployee(...)</c> and <c>employee.ToResponse()</c>.
/// Kept as plain static methods (no HTTP code) so they can be unit-tested without starting a server.
/// </summary>
public static class EmployeeMapping
{
    /// <summary>
    /// Builds the right Employee subclass for the requested role.
    /// <paramref name="id"/> and <paramref name="joinedOn"/> come from the server, never from the client.
    /// </summary>
    public static Employee ToEmployee(this EmployeeRequest request, int id, DateTime joinedOn)
    {
        ArgumentNullException.ThrowIfNull(request);
        string name = request.Name.Trim();

        // 1.4 switch expression + 2.1 object initializers. Id, Name and JoinedOn are init-only,
        // so they have to be set here, at creation.
        Employee employee = request.Role switch
        {
            EmployeeRole.Manager => new Manager { Id = id, Name = name, DepartmentCode = request.DepartmentCode, JoinedOn = joinedOn },
            EmployeeRole.Intern => new Intern { Id = id, Name = name, DepartmentCode = request.DepartmentCode, JoinedOn = joinedOn },
            EmployeeRole.FullTime => new FullTimeEmployee { Id = id, Name = name, DepartmentCode = request.DepartmentCode, JoinedOn = joinedOn },
            _ => throw new ValidationException($"Unknown role '{request.Role}'.")
        };

        employee.MonthlySalary = request.MonthlySalary;   // the setter rejects negatives (Week 1 rule)
        employee.Email = request.Email;
        employee.ManagerId = request.ManagerId;
        employee.Location = request.Location switch
        {
            WorkLocationType.Remote => new Remote(),
            WorkLocationType.Hybrid => new Hybrid(),
            WorkLocationType.Office => new Office(),
            _ => throw new ValidationException($"Unknown location '{request.Location}'.")
        };
        return employee;
    }

    /// <summary>Flattens an Employee (any subclass) into the JSON response shape.</summary>
    public static EmployeeResponse ToResponse(this Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        // 3.6 Type patterns: the class decides the role / location value
        EmployeeRole role = employee switch
        {
            Manager => EmployeeRole.Manager,
            Intern => EmployeeRole.Intern,
            _ => EmployeeRole.FullTime
        };
        WorkLocationType location = employee.Location switch
        {
            Remote => WorkLocationType.Remote,
            Hybrid => WorkLocationType.Hybrid,
            _ => WorkLocationType.Office
        };

        return new EmployeeResponse(
            employee.Id,
            employee.Name,
            role,
            employee.DepartmentCode,
            employee.Email,
            employee.ManagerId,
            location,
            employee.MonthlySalary,
            employee.MonthlyPay(),
            employee.CalculateBonus(),   // 2.3 virtual: Manager and Intern use their own version
            employee.JoinedOn);
    }
}
