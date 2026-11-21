namespace EmployeeApp.Core.Models;

/// <summary>
/// 10.1 A login account (who can call the API). Not the same thing as an <see cref="Employee"/>.
/// Named AppUser, not User, because every controller already has a <c>User</c> property (the logged-in ClaimsPrincipal).
/// The password is never stored, only its salted hash.
/// </summary>
public class AppUser : IEntity
{
    public int Id { get; init; }
    public required string Email { get; init; }          // stored lower-case, so login is case-insensitive
    public required string PasswordHash { get; set; }
    public required string Role { get; set; }            // "Admin" or "User": goes into the JWT as the "role" claim
    public DateTime CreatedOn { get; init; }
}
