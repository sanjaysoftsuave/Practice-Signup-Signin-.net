using EmployeeApp.Core.Exceptions;
using EmployeeApp.Core.Models;
using EmployeeApp.Core.Repositories;
using Microsoft.AspNetCore.Identity;

namespace EmployeeApp.Api.Auth;

/// <summary>Thrown when someone signs up with an email that already has an account. The controller turns it into 409.</summary>
public class DuplicateEmailException(string email) : Exception($"An account for {email} already exists.");

/// <summary>
/// 10.1 Authentication rules: sign up (create an account) and log in (check the password, issue a token).
/// Passwords are hashed with ASP.NET Core Identity's PasswordHasher: PBKDF2 with a random salt per user and
/// many iterations, so two users with the same password get different hashes, and guessing is slow.
/// Never store or log the plain password. (Java: Spring Security's BCryptPasswordEncoder.)
/// </summary>
public class AuthService(IRepository<AppUser> users, TokenService tokens, TimeProvider clock)
{
    private readonly PasswordHasher<AppUser> _hasher = new();

    /// <summary>Creates a "User" account. Admins aren't created through sign up (see <see cref="EnsureAdmin"/>).</summary>
    public AppUser SignUp(string email, string password)
    {
        string normalized = Normalize(email);
        ValidatePassword(password);
        if (FindByEmail(normalized) is not null)
            throw new DuplicateEmailException(normalized);

        return Create(normalized, password, Roles.User);
    }

    /// <summary>
    /// Returns a token, or null when the email or password is wrong. The caller can't tell WHICH one was wrong,
    /// on purpose: a different answer for "no such email" would tell an attacker which emails have accounts.
    /// </summary>
    public TokenResponse? Login(string email, string password)
    {
        AppUser? user = FindByEmail(Normalize(email));
        if (user is null)
            return null;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
            return null;

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // The hash used older settings (fewer iterations): store a fresh one while we know the password.
            user.PasswordHash = _hasher.HashPassword(user, password);
            users.Update(user);
        }

        return tokens.CreateToken(user);
    }

    /// <summary>
    /// Startup only: makes sure one Admin account exists. Its password comes from configuration
    /// (user-secrets "Auth:AdminPassword"), never from source code.
    /// </summary>
    public void EnsureAdmin(string email, string password)
    {
        string normalized = Normalize(email);
        if (FindByEmail(normalized) is null)
            Create(normalized, password, Roles.Admin);
    }

    /// <summary>
    /// Rules the [StringLength] attribute can't express. Min length 8 is checked here too, so the
    /// rule holds even if this service is called without going through the controller.
    /// </summary>
    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            throw new ValidationException("Password must be at least 8 characters.");
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            throw new ValidationException("Password must contain at least one letter and one digit.");
    }

    private AppUser Create(string email, string password, string role)
    {
        int nextId = users.GetAll().Select(u => u.Id).DefaultIfEmpty(0).Max() + 1;
        var user = new AppUser
        {
            Id = nextId,
            Email = email,
            PasswordHash = "",
            Role = role,
            CreatedOn = clock.GetUtcNow().UtcDateTime,
        };
        user.PasswordHash = _hasher.HashPassword(user, password);
        users.Add(user);
        return user;
    }

    // Training-sized: reads every user. A real app would query by email (Day 9: an indexed WHERE email = @email).
    private AppUser? FindByEmail(string normalizedEmail) =>
        users.GetAll().FirstOrDefault(u => u.Email == normalizedEmail);

    private static string Normalize(string email) => (email ?? "").Trim().ToLowerInvariant();
}
