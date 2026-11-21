using EmployeeApp.Api.Auth;
using EmployeeApp.Core.Exceptions;
using EmployeeApp.Core.Models;
using EmployeeApp.Core.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EmployeeApp.Tests;

/// <summary>10.x Unit tests for the authentication rules: no web server, no database.</summary>
public class AuthServiceTests
{
    internal static readonly JwtOptions TestJwt = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        Key = "test-signing-key-that-is-at-least-32-characters",
        ExpiryMinutes = 60,
    };

    private static readonly DateTimeOffset Now = new(2026, 11, 19, 10, 0, 0, TimeSpan.Zero);

    private readonly InMemoryRepository<AppUser> _users = new();
    private readonly AuthService _auth;

    public AuthServiceTests()
    {
        var clock = new FixedClock(Now);
        _auth = new AuthService(_users, new TokenService(Options.Create(TestJwt), clock), clock);
    }

    [Fact]
    public void SignUp_NewEmail_CreatesUserRoleAndStoresOnlyAHash()
    {
        AppUser user = _auth.SignUp("  Priya@Example.com ", "Secret123");

        Assert.Equal("priya@example.com", user.Email);     // trimmed and lower-case
        Assert.Equal(Roles.User, user.Role);               // sign-up never gives Admin
        Assert.NotEqual("Secret123", user.PasswordHash);
        Assert.DoesNotContain("Secret123", user.PasswordHash);
        Assert.Single(_users.GetAll());
    }

    [Fact]
    public void SignUp_SamePasswordTwice_GivesDifferentHashes()
    {
        // A random salt per user: equal passwords don't produce equal hashes.
        AppUser a = _auth.SignUp("a@example.com", "Secret123");
        AppUser b = _auth.SignUp("b@example.com", "Secret123");

        Assert.NotEqual(a.PasswordHash, b.PasswordHash);
    }

    [Fact]
    public void SignUp_EmailAlreadyUsedWithDifferentCase_Throws()
    {
        _auth.SignUp("priya@example.com", "Secret123");

        Assert.Throws<DuplicateEmailException>(() => _auth.SignUp("PRIYA@example.com", "Other456"));
    }

    [Theory]
    [InlineData("short1")]       // under 8 characters
    [InlineData("onlyletters")]  // no digit
    [InlineData("12345678")]     // no letter
    [InlineData("")]
    public void SignUp_WeakPassword_ThrowsValidationException(string password)
    {
        Assert.Throws<ValidationException>(() => _auth.SignUp("priya@example.com", password));
        Assert.Empty(_users.GetAll());
    }

    [Fact]
    public void Login_CorrectPassword_ReturnsTokenWithIdEmailRoleAndExpiry()
    {
        AppUser user = _auth.SignUp("priya@example.com", "Secret123");

        TokenResponse? token = _auth.Login("Priya@Example.com", "Secret123");

        Assert.NotNull(token);
        Assert.Equal("Bearer", token.TokenType);
        Assert.Equal(Now.UtcDateTime.AddMinutes(60), token.ExpiresAt);

        // Read the payload back (no key needed to READ it: a JWT is signed, not encrypted).
        JsonWebToken jwt = new JsonWebTokenHandler().ReadJsonWebToken(token.AccessToken);
        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal("priya@example.com", jwt.GetClaim("email").Value);
        Assert.Equal(Roles.User, jwt.GetClaim("role").Value);
        Assert.Equal("test-issuer", jwt.Issuer);
        Assert.Contains("test-audience", jwt.Audiences);
        Assert.DoesNotContain("Secret123", jwt.EncodedPayload);
    }

    [Fact]
    public void Login_WrongPassword_ReturnsNull()
    {
        _auth.SignUp("priya@example.com", "Secret123");

        Assert.Null(_auth.Login("priya@example.com", "Wrong999"));
    }

    [Fact]
    public void Login_UnknownEmail_ReturnsNull_SameAsWrongPassword()
    {
        Assert.Null(_auth.Login("nobody@example.com", "Secret123"));
    }

    [Fact]
    public void EnsureAdmin_CreatesOneAdminAndIsSafeToRunAgain()
    {
        _auth.EnsureAdmin("admin@employeeapp.local", "Admin1234");
        _auth.EnsureAdmin("admin@employeeapp.local", "Admin1234");   // every app start calls it

        AppUser admin = Assert.Single(_users.GetAll());
        Assert.Equal(Roles.Admin, admin.Role);
        Assert.Equal(Roles.Admin, new JsonWebTokenHandler()
            .ReadJsonWebToken(_auth.Login("admin@employeeapp.local", "Admin1234")!.AccessToken)
            .GetClaim("role").Value);
    }
}

/// <summary>A clock that always says the same time, so expiry tests don't depend on when they run.</summary>
internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
