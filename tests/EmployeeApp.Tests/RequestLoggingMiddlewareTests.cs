using System.Security.Claims;
using EmployeeApp.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EmployeeApp.Tests;

/// <summary>
/// 7.4 The middleware tested ALONE: no web server. A DefaultHttpContext is a fake request/response,
/// and "next" is a small lambda that plays the part of the rest of the pipeline.
/// </summary>
public class RequestLoggingMiddlewareTests
{
    private readonly ListLogger _logger = new();

    private Task RunAsync(HttpContext context, RequestDelegate next) =>
        new RequestLoggingMiddleware(next, _logger).InvokeAsync(context);

    private static DefaultHttpContext Request(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        return context;
    }

    [Fact]
    public async Task SuccessfulRequest_LogsMethodPathStatusAndUser_AtInformation()
    {
        var context = Request("GET", "/api/employees");

        await RunAsync(context, ctx =>
        {
            // pretend authentication ran and the controller answered 200
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "2")], "Bearer"));
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        });

        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("HTTP GET /api/employees responded 200", entry.Message);
        Assert.Contains("(user 2)", entry.Message);
    }

    [Theory]
    [InlineData(401, LogLevel.Warning)]
    [InlineData(403, LogLevel.Warning)]
    [InlineData(404, LogLevel.Warning)]
    [InlineData(500, LogLevel.Error)]
    public async Task ErrorStatus_UsesMatchingLogLevel(int status, LogLevel expected)
    {
        await RunAsync(Request("POST", "/api/employees"), ctx =>
        {
            ctx.Response.StatusCode = status;
            return Task.CompletedTask;
        });

        Assert.Equal(expected, Assert.Single(_logger.Entries).Level);
    }

    [Fact]
    public async Task NoToken_LogsAnonymous()
    {
        await RunAsync(Request("GET", "/api/employees"), _ => Task.CompletedTask);

        Assert.Contains("(user anonymous)", Assert.Single(_logger.Entries).Message);
    }

    [Fact]
    public async Task NeverLogsTokenBodyOrQueryString()
    {
        var context = Request("POST", "/api/auth/login");
        context.Request.QueryString = new QueryString("?password=Secret123");
        context.Request.Headers.Authorization = "Bearer eyJsecret-token";

        await RunAsync(context, _ => Task.CompletedTask);

        string message = Assert.Single(_logger.Entries).Message;
        Assert.DoesNotContain("Secret123", message);
        Assert.DoesNotContain("eyJsecret-token", message);
    }

    [Fact]
    public async Task Crash_LogsErrorAndRethrows()
    {
        var crash = new InvalidOperationException("boom");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RunAsync(Request("GET", "/api/employees"), _ => throw crash));

        Assert.Same(crash, thrown);   // the SAME exception goes on up; the middleware doesn't swallow it
        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(crash, entry.Exception);
    }
}

/// <summary>A fake ILogger that keeps every log line in a list, so a test can check it.</summary>
internal sealed class ListLogger : ILogger<RequestLoggingMiddleware>
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, formatter(state, exception), exception));

    public bool IsEnabled(LogLevel logLevel) => true;
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}
