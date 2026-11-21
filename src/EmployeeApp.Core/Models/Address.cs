namespace EmployeeApp.Core.Models;

/// <summary>
/// 2.5 Record: pure data compared by value. Two addresses with the same city and pincode are equal.
/// Positional syntax makes both properties init-only (immutable); use <c>with</c> to change a copy.
/// </summary>
public record Address(string City, string Pincode);
