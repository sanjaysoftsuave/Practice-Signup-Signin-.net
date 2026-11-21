namespace EmployeeApp.Core.Exceptions;

/// <summary>
/// 1.8 Custom exception for business-rule violations the user can fix (bad salary, bad raise %).
/// Kept separate from ArgumentNullException, which signals a programming bug, not bad input.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
