using System.ComponentModel.DataAnnotations;

namespace EmployeeApp.Api.Contracts;

/// <summary>Body of POST /api/auth/signup and POST /api/auth/login.</summary>
public class CredentialsRequest
{
    /// <example>priya@example.com</example>
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = "";

    /// <summary>At least 8 characters, with at least one letter and one digit.</summary>
    /// <example>Secret123</example>
    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = "";
}

/// <summary>An account, as the API shows it. There is no password or hash here, ever.</summary>
public record UserResponse(int Id, string Email, string Role, DateTime CreatedOn);

/// <summary>What GET /api/auth/me reads back from the token's claims.</summary>
public record CurrentUserResponse(string? Id, string? Email, IReadOnlyList<string> Roles);
