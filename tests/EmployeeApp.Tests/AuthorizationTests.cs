using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EmployeeApp.Api.Auth;
using EmployeeApp.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace EmployeeApp.Tests;

/// <summary>
/// 10.5 Endpoint verification: the whole API runs in memory (WebApplicationFactory = a test server, no port),
/// and each test sends real HTTP requests through the real pipeline: authentication → authorization → controller.
/// Proves the three cases: 401 (who are you?), 403 (you may not), and success.
/// </summary>
public sealed class AuthorizationTests : IDisposable
{
    private const string AdminEmail = "admin@test.local";
    private const string AdminPassword = "Admin1234";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthorizationTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host => host
            // Not "Development": user-secrets aren't loaded, so tests never touch your real PostgreSQL or keys.
            .UseEnvironment("Testing")
            .UseSetting("ConnectionStrings:EmployeeDb", "")
            .UseSetting("Jwt:Issuer", AuthServiceTests.TestJwt.Issuer)
            .UseSetting("Jwt:Audience", AuthServiceTests.TestJwt.Audience)
            .UseSetting("Jwt:Key", AuthServiceTests.TestJwt.Key)
            .UseSetting("Auth:AdminEmail", AdminEmail)
            .UseSetting("Auth:AdminPassword", AdminPassword));
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ---------------- 401: not authenticated ----------------

    [Fact]
    public async Task GetEmployees_NoToken_Returns401()
    {
        var response = await _client.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.Single().Scheme);   // "send me a Bearer token"
    }

    [Fact]
    public async Task GetEmployees_TamperedToken_Returns401()
    {
        string token = await LoginAsync(AdminEmail, AdminPassword);
        string tampered = token[..^4] + (token.EndsWith("AAAA") ? "BBBB" : "AAAA");   // break the signature

        var response = await SendAsync(HttpMethod.Get, "/api/employees", tampered);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetEmployees_ExpiredToken_Returns401()
    {
        // A correctly signed token that expired an hour ago.
        var twoHoursAgo = new FixedClock(DateTimeOffset.UtcNow.AddHours(-2));
        string expired = new TokenService(Options.Create(AuthServiceTests.TestJwt), twoHoursAgo)
            .CreateToken(new AppUser { Id = 1, Email = AdminEmail, PasswordHash = "", Role = Roles.Admin })
            .AccessToken;

        var response = await SendAsync(HttpMethod.Get, "/api/employees", expired);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email = AdminEmail, password = "Wrong9999" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------------- 403: authenticated, but not allowed ----------------

    [Fact]
    public async Task SignedUpUser_CanRead_ButCannotHire_Returns403()
    {
        var signUp = await _client.PostAsJsonAsync("/api/auth/signup", new { email = "priya@example.com", password = "Secret123" });
        Assert.Equal(HttpStatusCode.Created, signUp.StatusCode);
        string token = await LoginAsync("priya@example.com", "Secret123");

        var read = await SendAsync(HttpMethod.Get, "/api/employees", token);
        var hire = await SendAsync(HttpMethod.Post, "/api/employees", token, NewHire);
        var fire = await SendAsync(HttpMethod.Delete, "/api/employees/5", token);

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, hire.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, fire.StatusCode);
    }

    // ---------------- Success: Admin ----------------

    [Fact]
    public async Task Admin_CanHire_Returns201()
    {
        string token = await LoginAsync(AdminEmail, AdminPassword);

        var response = await SendAsync(HttpMethod.Post, "/api/employees", token, NewHire);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsClaimsFromTheToken()
    {
        string token = await LoginAsync(AdminEmail, AdminPassword);

        var response = await SendAsync(HttpMethod.Get, "/api/auth/me", token);
        var me = await response.Content.ReadFromJsonAsync<MeBody>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(AdminEmail, me!.Email);
        Assert.Equal([Roles.Admin], me.Roles);
    }

    [Fact]
    public async Task SignUp_SameEmailTwice_Returns409()
    {
        var body = new { email = "dup@example.com", password = "Secret123" };
        await _client.PostAsJsonAsync("/api/auth/signup", body);

        var second = await _client.PostAsJsonAsync("/api/auth/signup", body);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // ---------------- Helpers ----------------

    private static readonly object NewHire = new { name = "Divya", departmentCode = "ENG", monthlySalary = 70000 };

    private record LoginBody(string AccessToken);
    private record MeBody(string Email, List<string> Roles);

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginBody>())!.AccessToken;
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);   // Authorization: Bearer eyJ...
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return _client.SendAsync(request);
    }
}
