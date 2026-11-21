using System.Diagnostics;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EmployeeApp.Api.Middleware;

/// <summary>
/// 7.4 Our own middleware: writes ONE log line per request, e.g.
///   HTTP POST /api/employees responded 403 in 2 ms (user 2)
///
/// How a middleware works:
///   1. ASP.NET creates this class ONCE, at startup, and passes in <c>next</c>: the rest of the pipeline.
///   2. For every request it calls <see cref="InvokeAsync"/>.
///   3. Code BEFORE <c>await next(context)</c> runs on the way IN; code AFTER it runs on the way OUT.
///
/// SAFE logging: only the method, path, status, time and user id.
/// Never the body (it has passwords), never the Authorization header (the token), never the query string.
/// Express: app.use(async (req, res, next) => { ...; await next(); ... })
/// </summary>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // ---------- On the way IN ----------
        long start = Stopwatch.GetTimestamp();

        try
        {
            await next(context);   // run the rest: authentication → authorization → controller
        }
        catch (Exception ex)
        {
            // Something below crashed. Log it with the same details, then let ASP.NET send the 500.
            logger.LogError(ex, "HTTP {Method} {Path} failed after {ElapsedMs} ms",
                context.Request.Method, context.Request.Path, ElapsedMs(start));
            throw;   // 1.8 'throw;' keeps the original stack trace
        }

        // ---------- On the way OUT ----------
        int status = context.Response.StatusCode;

        // The authentication middleware ran INSIDE next(), so context.User is filled in by now.
        string user = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "anonymous";

        // 7.6 Level depends on the result: 5xx = our bug (Error), 4xx = the client's mistake (Warning).
        LogLevel level = status >= 500 ? LogLevel.Error
                       : status >= 400 ? LogLevel.Warning
                       : LogLevel.Information;

        // 7.6 A message TEMPLATE, not string interpolation ($"..."): {Method}, {Path}... become searchable
        // fields in log tools (e.g. "show every request where Status = 403").
        logger.Log(level, "HTTP {Method} {Path} responded {Status} in {ElapsedMs} ms (user {User})",
            context.Request.Method, context.Request.Path, status, ElapsedMs(start), user);
    }

    private static long ElapsedMs(long start) => (long)Stopwatch.GetElapsedTime(start).TotalMilliseconds;
}

/// <summary>So Program.cs can say <c>app.UseRequestLogging()</c>, like the built-in <c>app.UseAuthentication()</c>.</summary>
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestLoggingMiddleware>();
}
