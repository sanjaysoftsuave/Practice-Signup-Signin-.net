namespace EmployeeApp.Core.Exceptions;

/// <summary>1.8 Custom exception: carries the missing Id so callers can report it.</summary>
public class EmployeeNotFoundException : Exception
{
    public int EmployeeId { get; }

    public EmployeeNotFoundException(int employeeId)
        : base($"Employee #{employeeId} was not found.")
    {
        EmployeeId = employeeId;
    }
}

public class DepartmentNotFoundException : Exception
{
    public string Code { get; }

    public DepartmentNotFoundException(string code)
        : base($"Department '{code}' was not found.")
    {
        Code = code;
    }
}
