// 6.2 The Web API's starting point. Two halves:
//   1. builder: register services (what the app HAS)       Spring: @Bean / @Service
//   2. app:     build the request pipeline (what each request GOES THROUGH)   Express: app.use(...)
using System.Text.Json.Serialization;
using EmployeeApp.Api.Auth;
using EmployeeApp.Api.Middleware;
using EmployeeApp.Core.Models;
using EmployeeApp.Core.Repositories;
using EmployeeApp.Core.Services;
using EmployeeApp.Infrastructure;
using EmployeeApp.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. Services ----------

builder.Services
    .AddControllers()   // 6.3 finds every [ApiController] class
    .AddJsonOptions(options =>
        // 6.4 Enums as text ("Manager"), not numbers (1). Numbers are rejected, so 7 can't sneak in.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

builder.Services.AddProblemDetails();   // 6.5 errors use the standard ProblemDetails JSON shape
builder.Services.AddOpenApi(options =>   // 6.6 generates /openapi/v1.json from the controllers
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());   // 10.5 adds the "Authorize" button

// ---------- 10. Authentication (who are you?) and authorization (what may you do?) ----------

// 7.7 Options pattern + 10.4 secrets: the "Jwt" section. Jwt:Key comes from user-secrets.
// ValidateOnStart: a missing or short key stops the app at startup with a clear message, not at the first login.
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)   // read "Authorization: Bearer <token>" on every request
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;   // keep the short claim names ("sub", "role") from the token
        options.TokenValidationParameters.RoleClaimType = "role";   // [Authorize(Roles/Policy)] checks this claim
        options.TokenValidationParameters.NameClaimType = "email";
    });

// The validation settings need JwtOptions, which DI only has after the app is built: configure them "later" with DI.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwt) =>
    {
        var p = options.TokenValidationParameters;
        p.ValidIssuer = jwt.Value.Issuer;        // reject tokens made by someone else
        p.ValidAudience = jwt.Value.Audience;    // reject tokens meant for another API
        p.IssuerSigningKey = TokenService.CreateSigningKey(jwt.Value.Key);   // reject tokens with a wrong signature
        p.ClockSkew = TimeSpan.FromSeconds(30);  // default is 5 minutes of grace after "exp"
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Admin));   // 10.3 a named rule

builder.Services.AddSingleton(TimeProvider.System);   // the clock, injectable so tests can control "now"
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<AuthService>();

// 7.5 Configuration: the connection string comes from user-secrets or an environment variable,
// NEVER from a committed file. appsettings.json only has an empty placeholder.
string? connectionString = builder.Configuration.GetConnectionString("EmployeeDb");
bool useDatabase = !string.IsNullOrWhiteSpace(connectionString);

if (useDatabase)
{
    // 9.x PostgreSQL through EF Core. Scoped = one DbContext + repositories + service per HTTP request.
    builder.Services.AddEmployeeDatabase(connectionString!);
    builder.Services.AddScoped<EmployeeService>();
}
else
{
    // No connection string yet: keep everything in memory.
    // 7.2 Singleton = ONE shared instance for the whole app, so the data survives between requests.
    builder.Services.AddSingleton<IRepository<Employee>, InMemoryRepository<Employee>>();
    builder.Services.AddSingleton<IRepository<Department>, InMemoryRepository<Department>>();
    builder.Services.AddSingleton<IRepository<AppUser>, InMemoryRepository<AppUser>>();
    builder.Services.AddSingleton<EmployeeService>();
}

var app = builder.Build();

if (useDatabase)
{
    // Outside a request there's no scope, so create one to get the scoped DbContext and service.
    using var scope = app.Services.CreateScope();
    if (app.Environment.IsDevelopment())
    {
        // 9.3 Applies any migrations not yet run: creates the database and tables the first time.
        // Production would run `dotnet ef database update` (or a SQL script) as a deliberate step instead.
        scope.ServiceProvider.GetRequiredService<EmployeeDbContext>().Database.Migrate();
    }

    var service = scope.ServiceProvider.GetRequiredService<EmployeeService>();
    if (service.GetDepartments().Count == 0)
        SampleData.Seed(service);   // only into an empty database, so restarts keep your changes
}
else
{
    // Same starting data as the console app (3 departments, 5 employees)
    SampleData.Seed(app.Services.GetRequiredService<EmployeeService>());
}

// 10.4 One Admin account, from configuration. The password is a user-secret, not in any committed file.
using (var scope = app.Services.CreateScope())
{
    string adminEmail = app.Configuration["Auth:AdminEmail"] ?? "admin@employeeapp.local";
    string? adminPassword = app.Configuration["Auth:AdminPassword"];
    if (string.IsNullOrWhiteSpace(adminPassword))
        app.Logger.LogWarning("Auth:AdminPassword is not set, so no Admin account exists. Only sign-up users (read-only) can log in.");
    else
        scope.ServiceProvider.GetRequiredService<AuthService>().EnsureAdmin(adminEmail, adminPassword);
}

app.Logger.LogInformation("Storage: {Storage}",
    useDatabase ? "PostgreSQL" : "in-memory (ConnectionStrings:EmployeeDb is not set)");

// ---------- 2. Request pipeline (order matters: Day 7) ----------

// 7.4 OUR middleware goes FIRST, so it wraps everything below: it times the whole request and also
// logs requests that the auth middleware rejects with 401/403 (they never reach a controller).
app.UseRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();   // GET /openapi/v1.json: the machine-readable API description
    app.UseSwaggerUI(options =>
    {
        // GET /swagger: a web page that reads that JSON and lets you try every endpoint
        options.SwaggerEndpoint("/openapi/v1.json", "EmployeeApp API v1");
    });
}

app.UseHttpsRedirection();

// 7.3 Order matters: first find out WHO the caller is (read the token), then check WHAT they may do.
// Swap these two and every [Authorize] endpoint answers 401, because nobody has been identified yet.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();   // connects URLs like /api/employees/2 to controller actions

app.Run();   // starts Kestrel, the built-in web server, and waits for requests

/// <summary>Makes the auto-generated Program class visible to the integration tests (WebApplicationFactory&lt;Program&gt;).</summary>
public partial class Program;
