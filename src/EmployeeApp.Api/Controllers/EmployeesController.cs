using EmployeeApp.Api.Auth;
using EmployeeApp.Api.Contracts;
using EmployeeApp.Core.Exceptions;
using EmployeeApp.Core.Models;
using EmployeeApp.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeApp.Api.Controllers;

/// <summary>
/// CRUD endpoints for employees.
/// 6.3 [ApiController] turns on API behaviour: automatic 400 for invalid bodies, [FromBody] inferred
/// for complex types, ProblemDetails for errors. Java: @RestController + @RequestMapping("/api/employees").
/// 7.1 preview: the EmployeeService arrives through the constructor (dependency injection); Program.cs
/// registered it. The controller never calls <c>new EmployeeService(...)</c> itself.
/// 10.3 [Authorize] on the class: every endpoint needs a valid token (else 401). The write endpoints
/// also need the Admin role (AdminOnly policy): a logged-in "User" gets 403.
/// </summary>
[ApiController]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[Route("api/employees")]
[Produces("application/json")]
public class EmployeesController(EmployeeService service) : ControllerBase
{
    // ---------------- GET /api/employees  and  GET /api/employees?department=ENG ----------------

    /// <summary>Lists all employees, or only one department's employees.</summary>
    /// <param name="department">Optional department code filter, e.g. ENG.</param>
    /// <response code="200">The employees, ordered by Id.</response>
    /// <response code="400">The department code doesn't exist.</response>
    [HttpGet]
    [ProducesResponseType<IEnumerable<EmployeeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<IEnumerable<EmployeeResponse>> GetAll([FromQuery] string? department)
    {
        try
        {
            IEnumerable<Employee> staff = string.IsNullOrWhiteSpace(department)
                ? service.GetEmployees()
                : service.GetByDepartment(department);

            // 3.4 Select maps each Employee to a DTO. ToList runs it now, while we're still in the try.
            return Ok(staff.Select(e => e.ToResponse()).ToList());
        }
        catch (DepartmentNotFoundException ex)
        {
            return BadRequestProblem(ex);
        }
    }

    // ---------------- GET /api/employees/2 ----------------

    /// <summary>Gets one employee by Id.</summary>
    /// <response code="200">The employee.</response>
    /// <response code="404">No employee has this Id.</response>
    [HttpGet("{id:int}")]   // 6.3 route constraint: /api/employees/abc doesn't match, so it's a 404
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<EmployeeResponse> GetById(int id)   // 6.4 id is bound from the route
    {
        try
        {
            return service.GetEmployee(id).ToResponse();   // ActionResult<T>: returning T means 200 OK
        }
        catch (EmployeeNotFoundException ex)
        {
            return NotFoundProblem(ex);
        }
    }

    // ---------------- POST /api/employees ----------------

    /// <summary>Hires a new employee. The server assigns the Id.</summary>
    /// <response code="201">Created. The Location header holds the new employee's URL.</response>
    /// <response code="400">The body is invalid, or it breaks a business rule
    /// (unknown department or manager, intern salary over the cap).</response>
    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Consumes("application/json")]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<EmployeeResponse> Create(EmployeeRequest request)   // 6.4 [FromBody] is inferred
    {
        try
        {
            Employee employee = request.ToEmployee(service.NextEmployeeId(), DateTime.Today);
            service.Hire(employee);

            // 6.5 201 Created + "Location: /api/employees/6" + the new employee in the body.
            return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee.ToResponse());
        }
        // 1.8 exception filter + 3.6 'or' pattern: one catch for every "the client sent bad data" error.
        // An unknown ManagerId is a mistake IN THE BODY, so it's 400 here, not 404.
        catch (Exception ex) when (ex is ValidationException or DepartmentNotFoundException or EmployeeNotFoundException)
        {
            return BadRequestProblem(ex);
        }
    }

    // ---------------- PUT /api/employees/2 ----------------

    /// <summary>Replaces an employee completely. Fields you leave out go back to their defaults.</summary>
    /// <response code="204">Updated. No body.</response>
    /// <response code="400">The body is invalid, or it breaks a business rule.</response>
    /// <response code="404">No employee has this Id.</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult Update(int id, EmployeeRequest request)
    {
        // Step 1: does the employee in the URL exist? If not, it's 404.
        Employee existing;
        try
        {
            existing = service.GetEmployee(id);
        }
        catch (EmployeeNotFoundException ex)
        {
            return NotFoundProblem(ex);
        }

        // Step 2: apply the new version. JoinedOn is kept from the original; the client can't change it.
        try
        {
            service.Update(request.ToEmployee(id, existing.JoinedOn));
            return NoContent();   // 6.5 204: success, nothing to send back
        }
        catch (Exception ex) when (ex is ValidationException or DepartmentNotFoundException or EmployeeNotFoundException)
        {
            return BadRequestProblem(ex);
        }
    }

    // ---------------- DELETE /api/employees/2 ----------------

    /// <summary>Removes an employee.</summary>
    /// <response code="204">Deleted. No body.</response>
    /// <response code="404">No employee has this Id.</response>
    /// <response code="409">Other employees still report to this person.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public IActionResult Delete(int id)
    {
        try
        {
            service.Fire(id);
            return NoContent();
        }
        catch (EmployeeNotFoundException ex)
        {
            return NotFoundProblem(ex);
        }
        catch (InvalidOperationException ex)
        {
            // 6.5 409 Conflict: the request is valid, but the current state of the data blocks it.
            return Problem(title: "Conflict", detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    // ---------------- Helpers ----------------
    // Problem(...) returns a ProblemDetails body (RFC 9457), the standard JSON error shape:
    // { "type": ..., "title": ..., "status": 404, "detail": "Employee #99 was not found.", "traceId": ... }
    // Day 8 moves these try/catch blocks into ONE central exception handler.

    private ObjectResult NotFoundProblem(Exception ex) =>
        Problem(title: "Not found", detail: ex.Message, statusCode: StatusCodes.Status404NotFound);

    private ObjectResult BadRequestProblem(Exception ex) =>
        Problem(title: "Invalid request", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
}
