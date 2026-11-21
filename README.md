# EmployeeApp — Employee & Department Management

DotNet Training, Week 1 capstone. A console application that manages employees and
departments in memory and can save them to a JSON file. It covers the whole week, lessons 1.2 to 4.5:
C# fundamentals, OOP, collections, generics, delegates and events, LINQ, extension methods, pattern
matching, async/await with cancellation, xUnit tests, and the build tooling.

**Week 2** adds an ASP.NET Core Web API (`src/EmployeeApp.Api`) on top of the same `EmployeeApp.Core`.
See [Week 2: Web API](#week-2-web-api) below.

## Requirements

- .NET 10 SDK (`dotnet --version` → 10.x)
- Optional: Visual Studio 2026 (the version this was set up for). An older Visual Studio 2022 may not be able
  to build `net10.0` projects even with the .NET 10 SDK installed; if it refuses, use the command line below.

## Open in Visual Studio

1. Open **`EmployeeApp.slnx`** (File → Open → Project/Solution).
   On a Visual Studio 2022 that can't open `.slnx`, open **`vs2022\EmployeeApp.sln`** instead.
   It holds the same 3 projects. It sits in its own folder because two solution files side by side
   make a plain `dotnet build` fail with *MSB1011: more than one project or solution file*.
2. Choose the startup project (shown in bold in Solution Explorer): right-click a project →
   **Set as Startup Project**. `EmployeeApp.Api` is listed first in `EmployeeApp.slnx` (Week 2), and
   `EmployeeApp.Cli` is first in `vs2022\EmployeeApp.sln`.
3. Press **F5** (debug) or **Ctrl+F5** (run without debugging). The `EmployeeApp.Cli` profile in
   `src/EmployeeApp.Cli/Properties/launchSettings.json` starts the console app.
4. **Test → Test Explorer → Run All**. The tests show up after the first build: 83 test cases once each
   `[Theory]` row counts separately (36 from Week 1, 14 for the Day 6 API, 6 for the EF Core repository,
   11 for sign-up/login rules, 8 for the 401/403 endpoint checks, 8 for the logging middleware).

## Command line

Run everything from the `EmployeeApp` folder (where `EmployeeApp.slnx` is):

```powershell
dotnet build                                      # Debug build of all 5 projects (0 warnings: warnings are errors)
dotnet run --project src/EmployeeApp.Cli          # start the app
dotnet test                                       # build + run all xUnit tests
dotnet build -c Release                           # optimised Release build
dotnet publish src/EmployeeApp.Cli -c Release     # → src/EmployeeApp.Cli/bin/Release/net10.0/publish/
```

`dotnet run` prints `Using launch settings from ...launchSettings.json...` before the app starts.
That line comes from the SDK, not from the app.

The app starts with 3 departments (ENG, HR, SALES) and 5 employees already loaded.

### Where the save file goes

Options 9 and 10 use **`employees.json` in the app's output folder** (`AppContext.BaseDirectory`):
`src/EmployeeApp.Cli/bin/Debug/net10.0/employees.json` for a Debug build. It's the output folder rather
than the current directory because F5 in Visual Studio and `dotnet run` from different folders start in
different working directories, but they always run the same `.exe`. So it's always the same file.
`bin/` is git-ignored, and so is the save file.

## Week 2: Web API

### Day 6: Employee CRUD endpoints

`src/EmployeeApp.Api` is an ASP.NET Core Web API (controllers) over the Week 1 `EmployeeService`.
Data is still in memory: every restart goes back to the same 5 seeded employees. Day 9 adds a database.

**Run it**

- Visual Studio: set `EmployeeApp.Api` as the startup project, then press **F5**. The browser opens Swagger UI.
- Command line: `dotnet run --project src/EmployeeApp.Api --launch-profile http`, then open
  <http://localhost:5286/swagger>.

| URL | What it is |
|---|---|
| `http://localhost:5286/swagger` | Swagger UI: try every endpoint in the browser (Development only) |
| `http://localhost:5286/openapi/v1.json` | The OpenAPI document. A saved copy is in `docs/openapi-v1.json` |
| `src/EmployeeApp.Api/EmployeeApp.Api.http` | Success **and** failure requests; click "Send request" in VS |

**Files**

```
src/EmployeeApp.Api/
├── Program.cs                       ← registers services, builds the pipeline, starts the server
├── Controllers/EmployeesController.cs  ← the 5 endpoints; HTTP only, rules stay in Core
├── Contracts/EmployeeContracts.cs   ← DTOs: EmployeeRequest (in), EmployeeResponse (out), enums
├── Contracts/EmployeeMapping.cs     ← DTO ↔ Employee conversion (unit-tested)
├── EmployeeApp.Api.http             ← example requests
└── Properties/launchSettings.json   ← port 5286, opens /swagger
```

**Endpoints and status codes**

| Method | URL | Success | Failures |
|---|---|---|---|
| GET | `/api/employees` | 200 list | — |
| GET | `/api/employees?department=ENG` | 200 list | 400 unknown department |
| GET | `/api/employees/{id}` | 200 employee | 404 no such Id (or Id isn't a number) |
| POST | `/api/employees` | 201 + `Location` header + employee | 400 invalid body or business rule |
| PUT | `/api/employees/{id}` | 204 no body | 400 invalid body or rule, 404 no such Id |
| DELETE | `/api/employees/{id}` | 204 no body | 404 no such Id, 409 still manages people |

**Why each code**

| Code | Meaning | Used when |
|---|---|---|
| 200 OK | Here's what you asked for | Any successful GET |
| 201 Created | I made a new thing; here's where it lives | POST succeeded (`Location: /api/employees/6`) |
| 204 No Content | Done, nothing to send back | PUT or DELETE succeeded |
| 400 Bad Request | Your request is wrong; fix it and retry | Validation failed, unknown department/manager, intern salary > 50,000 |
| 404 Not Found | The thing in the URL doesn't exist | Wrong Id in the URL |
| 409 Conflict | Valid request, but the current data blocks it | Deleting a manager who still has reports |

All errors return a **ProblemDetails** JSON body (RFC 9457):

```json
{ "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5", "title": "Not found",
  "status": 404, "detail": "Employee #99 was not found.", "traceId": "00-..." }
```

Validation errors (from the `[Required]`, `[Range]` and `[EmailAddress]` attributes) list every field:

```json
{ "title": "One or more validation errors occurred.", "status": 400,
  "errors": { "Name": ["The Name field is required."],
              "MonthlySalary": ["The field MonthlySalary must be between 0 and 10000000."] } }
```

**PUT vs PATCH:** PUT replaces the whole employee, so a field you leave out goes back to its default
(e.g. no `managerId` means "no manager"). PATCH would change only the fields you send; it isn't built yet.

### PostgreSQL (set up early; full lessons on Day 9)

`src/EmployeeApp.Infrastructure` holds the data layer: EF Core + Npgsql (the PostgreSQL provider).

```
src/EmployeeApp.Infrastructure/
├── Data/EmployeeDbContext.cs           ← the EF Core "session": DbSet<Employee>, DbSet<Department>
├── Data/Configurations/*.cs            ← table/column rules (Fluent API), one class per entity
├── Data/Migrations/                    ← generated C# that creates the tables (InitialCreate)
├── Data/EmployeeDbContextFactory.cs    ← used only by the `dotnet ef` tool
├── Repositories/EfRepository.cs        ← IRepository<T> backed by the database
└── DependencyInjection.cs              ← AddEmployeeDatabase(connectionString)
```

**Which storage runs?** It depends on whether `ConnectionStrings:EmployeeDb` is set. The log line
`Storage: ...` at startup tells you which one is in use.

| Connection string | Storage | Data after restart |
|---|---|---|
| Not set (default) | In memory | Back to the 5 seeded employees |
| Set | PostgreSQL | Kept |

**Tables** (see `docs/schema.sql` for the exact SQL):

- `departments`: `id` primary key, `code` unique.
- `employees`: one table for all 3 employee classes, and `employee_type` says which class.
  - `location` is stored as text.
  - `address_city` and `address_pincode` are columns of this table.
  - Two foreign keys: `department_code → departments.code`, and `manager_id → employees.id`.
  - Both are `ON DELETE RESTRICT`.

**Connect to PostgreSQL** (one time). The password is stored with **user-secrets**, in your Windows
user profile, outside this repository, so it can never be committed:

```powershell
dotnet user-secrets set "ConnectionStrings:EmployeeDb" "Host=localhost;Port=5432;Database=employeeapp;Username=postgres;Password=YOUR_PASSWORD" --project src/EmployeeApp.Api
dotnet user-secrets list --project src/EmployeeApp.Api     # check it's set
```

Then start the API (F5). In Development it runs any pending migrations itself, creating the
`employeeapp` database and tables the first time, and seeds the 5 employees only if the tables are empty.

**Run the migrations by hand** (the production way). `dotnet ef` is installed as a local tool for this
repository (`dotnet-tools.json`), so run `dotnet tool restore` once:

```powershell
dotnet tool restore
dotnet ef database update --project src/EmployeeApp.Infrastructure --startup-project src/EmployeeApp.Infrastructure --connection "Host=localhost;Port=5432;Database=employeeapp;Username=postgres;Password=YOUR_PASSWORD"
```

**After changing the model**, add a new migration (this needs no database connection):

```powershell
dotnet ef migrations add <Name> --project src/EmployeeApp.Infrastructure --startup-project src/EmployeeApp.Infrastructure --output-dir Data/Migrations
```

**Known limitations (Day 6)**

- With no connection string, data is in memory and resets on restart.
- The repository is still synchronous (the Week 1 interface); Day 9 discusses `async` EF calls.
- The in-memory repository isn't thread-safe; two POSTs at the exact same moment could pick the same Id.
- Each action has its own try/catch; Day 8 replaces them with one central exception handler.
- `EfRepository` and the sign-up email check read whole tables; fine for training data, not for big tables.

### Day 7: Request-logging middleware

`src/EmployeeApp.Api/Middleware/RequestLoggingMiddleware.cs` is our own middleware. It writes one line per request:

```
info: HTTP GET /api/employees responded 200 in 45 ms (user 2)
warn: HTTP POST /api/employees responded 403 in 1 ms (user 2)
warn: HTTP GET /api/employees/99 responded 404 in 9 ms (user 1)
```

- **Safe:** only the method, path, status, time and user id. It never logs the body (passwords), the
  `Authorization` header (the token) or the query string.
- **Level by result:** 2xx/3xx = Information, 4xx = Warning, 5xx or a crash = Error.
- **Message template** (`"{Method} {Path}..."`, not `$"..."`), so log tools can filter by field.

**The pipeline** (`Program.cs`). Each request goes down through the list, and the response comes back up:

```
Request ─► RequestLogging (ours) ─► HttpsRedirection ─► Authentication ─► Authorization ─► Controller
Response ◄─ RequestLogging (ours) ◄─ ...                                                ◄─ Controller
```

Our middleware is **first**, so it times the whole request and also logs the 401/403 answers that
never reach a controller.

### Day 10: Sign up, login and JWT

**Authentication vs authorization**

| | Question | Fails with | Example |
|---|---|---|---|
| **Authentication** | *Who are you?* | **401 Unauthorized** | No token, a wrong signature, an expired token, a wrong password |
| **Authorization** | *What may you do?* | **403 Forbidden** | A logged-in "User" tries to hire someone |

401 means "I don't know you; log in (again)". 403 means "I know you, and the answer is no";
logging in again won't help. (The name "Unauthorized" for 401 is a historic mistake in HTTP: it means *unauthenticated*.)

**Who may do what**

| Endpoint | Anonymous | User (from sign up) | Admin |
|---|---|---|---|
| `POST /api/auth/signup`, `POST /api/auth/login` | ✅ | ✅ | ✅ |
| `GET /api/auth/me` | 401 | ✅ | ✅ |
| `GET /api/employees`, `GET /api/employees/{id}` | 401 | ✅ | ✅ |
| `POST`, `PUT`, `DELETE /api/employees...` | 401 | **403** | ✅ |

**The flow**

```
1. POST /api/auth/signup  {email, password}  → 201  (password stored as a salted PBKDF2 hash, never as text)
2. POST /api/auth/login   {email, password}  → 200  {"accessToken":"eyJ...","tokenType":"Bearer","expiresAt":...}
3. GET  /api/employees    Authorization: Bearer eyJ...   → 200
```

The token is a **JWT**: `header.payload.signature`. The payload holds the claims `sub` (user id), `email`,
`role`, `iss`, `aud` and `exp`. It is signed with HMAC-SHA256 and the secret `Jwt:Key`, so nobody can
change the role without breaking the signature. It is **not encrypted**: anyone can read it at jwt.io,
so it never contains a password.

**Files**

```
src/EmployeeApp.Api/
├── Auth/AuthService.cs              ← sign up (password rules, unique email, hashing), login, admin seed
├── Auth/TokenService.cs             ← creates the signed JWT
├── Auth/JwtOptions.cs               ← the "Jwt" settings (options pattern, validated at startup)
├── Auth/Roles.cs                    ← "Admin", "User" and the AdminOnly policy name
├── Auth/BearerSecuritySchemeTransformer.cs ← the "Authorize" button in Swagger UI
├── Controllers/AuthController.cs    ← /api/auth/signup, /login, /me
└── Program.cs                       ← AddAuthentication + AddJwtBearer, AddAuthorization policy,
                                       UseAuthentication BEFORE UseAuthorization
src/EmployeeApp.Core/Models/AppUser.cs  ← the account (Email, PasswordHash, Role)
src/EmployeeApp.Infrastructure/...      ← "users" table (unique email), migration AddUsers
```

**Configuration (no secrets in Git)**

| Setting | Where | Secret? |
|---|---|---|
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` | `appsettings.json` | No |
| `Jwt:Key` (32+ random characters) | user-secrets / environment variable | **Yes** |
| `Auth:AdminEmail` (`admin@employeeapp.local`) | `appsettings.json` | No |
| `Auth:AdminPassword` | user-secrets / environment variable | **Yes** |

One-time setup on a new machine:

```powershell
dotnet user-secrets set "Jwt:Key" "<32+ random characters>" --project src/EmployeeApp.Api
dotnet user-secrets set "Auth:AdminPassword" "<a strong password>" --project src/EmployeeApp.Api
```

Without `Jwt:Key` the API refuses to start (`ValidateOnStart`). Without `Auth:AdminPassword` it starts,
but no Admin exists, so nobody can hire. In production, use environment variables instead,
e.g. `Jwt__Key` (a double underscore replaces the colon).

**Try it**

- Swagger UI: call `POST /api/auth/login`, copy `accessToken`, click **Authorize**, paste it.
- `.http` file: put your admin password in `@adminPassword`, run the two login requests, then the rest.
  The AUTH section has the 401 and 403 examples. Don't commit the file with your real password in it.
- Tests: `AuthorizationTests` sends real HTTP requests through the whole pipeline and checks 401 (no token,
  bad signature, expired, wrong password), 403 (User hires or fires) and 201 (Admin hires).

**Known limitations (Day 10)**

- No refresh tokens and no logout: a token stays valid until `exp` (60 minutes). Bonus 10.6 covers why.
- In memory mode, signed-up accounts disappear on restart (the Admin is created again from config).
- No way to promote a User to Admin through the API yet; there is exactly one Admin.
- 401/403 responses from the auth handler have an empty body (only the `WWW-Authenticate` header), not ProblemDetails.

## Menu

```
1. List employees        5. Hire employee
2. Find employee by ID   6. Give raise
3. Search by name        7. Transfer employee
4. List a department     8. Payroll report
9. Save to file          10. Load from file
0. Exit
```

- **6. Give raise** also prints an `[audit]` line. That's the CLI's handler for the service's `SalaryChanged` event.
- **9. Save to file** writes every employee to `employees.json`, asynchronously, with a 5-second timeout.
- **10. Load from file** replaces the in-memory employees with the file's contents. The file is validated
  first: unknown department, duplicate IDs, unknown type, missing name or bad JSON. If any check fails,
  nothing is replaced.

## Project structure

```
EmployeeApp.slnx                   solution (VS 2026 / dotnet CLI)
vs2022/EmployeeApp.sln             classic solution, same 3 projects, for older VS 2022
Directory.Build.props              shared build settings for every project (4.5)
├── src/
│   ├── EmployeeApp.Core/          class library: all business logic, no Console code
│   │   ├── Models/                Employee (abstract), Manager, FullTimeEmployee, Intern,
│   │   │                          Department, Address (record), EmployeeSummary (record),
│   │   │                          IWorkLocation + Office/Remote/Hybrid, IEntity
│   │   ├── Repositories/          IRepository<T>, InMemoryRepository<T>
│   │   ├── Services/              EmployeeService, EmployeeExtensions, SalaryChangedEventArgs,
│   │   │                          SalaryBands, Codes (internal)
│   │   ├── Persistence/           IEmployeeStore, JsonEmployeeStore, EmployeeRecord (DTO)
│   │   └── Exceptions/            EmployeeNotFound, DepartmentNotFound, Validation
│   ├── EmployeeApp.Cli/           console app: menu, input, output only
│   ├── EmployeeApp.Api/           Week 2 Web API: Controllers/, Contracts/ (DTOs), Auth/ (JWT), Program.cs
│   └── EmployeeApp.Infrastructure/ Week 2 data layer: EF Core DbContext, mappings, migrations, EfRepository
└── tests/
    └── EmployeeApp.Tests/         xUnit: service, extensions, JSON store, DTO mapping, EF repository,
                                   AuthService (unit) and AuthorizationTests (real HTTP, 401/403)
```

> Why `EmployeeApp.Cli` and not `EmployeeApp.Console`? A project named `...Console` gets the
> namespace `EmployeeApp.Console`, and then `Console.WriteLine` refers to that namespace instead of
> `System.Console`, so it won't compile. That's a namespace clash (lesson 1.2).

## Sample inputs and outputs

All four transcripts below are copied from real runs. Menu lines and the `Choose an option:` prompt are
left out. Numbers use the machine's culture: en-IN here, so `1,60,000.00`. An en-US machine prints `160,000.00`.

### 1. List all employees, then the payroll report

Input: `1`, `8`, `0`

```
ID  Name      Role       Dept   Location   Monthly pay  Band
1   Ravi      Manager    ENG    Office     1,60,000.00  Senior
2   Priya     Full-time  ENG    Remote       92,000.00  Mid
3   Arun      Intern     ENG    Hybrid       21,000.00  Junior
4   Meena     Manager    HR     Office     1,20,000.00  Mid
5   Kumar     Full-time  SALES  Office       60,000.00  Junior

Monthly payroll by department
--------------------------------------------
ENG   Engineering     3 staff    2,73,000.00
HR    Human Res.      1 staff    1,20,000.00
SALES Sales           1 staff      60,000.00
--------------------------------------------
TOTAL                            4,53,000.00
```

Monthly pay = salary + location allowance (Remote +2,000, Hybrid +1,000). The report is built with
`GroupBy` + `ToDictionary` + the `TotalMonthlyPay()` extension method. The output is exactly what the old
`foreach` version printed.

### 2. Hire a manager, view them, give a raise, list the department

Input: `5`, `Divya`, `eng`, `abc`, `85000`, `2`, then `2`, `6`, then `6`, `6`, `10`, then `4`, `eng`, `0`

```
Name: Department code: Monthly salary: Please enter a number.
Monthly salary: Role (1 = Full-time, 2 = Manager, 3 = Intern): Hired #6 Divya (Manager, ENG, Office).

Employee ID: EmployeeSummary { Id = 6, Name = Divya, Role = Manager, Department = ENG, Location = Office, MonthlyPay = 85000, Bonus = 22000.00, Band = Mid, ManagerName = none }

Employee ID: Raise %: [audit] Divya (#6) salary 85,000.00 -> 93,500.00
New monthly salary: 93,500.00

Department code: ID  Name      Role       Dept   Location   Monthly pay  Band
1   Ravi      Manager    ENG    Office     1,60,000.00  Senior
2   Priya     Full-time  ENG    Remote       92,000.00  Mid
3   Arun      Intern     ENG    Hybrid       21,000.00  Junior
6   Divya     Manager    ENG    Office       93,500.00  Mid
```

This run shows:
- `TryParse` rejecting `abc`.
- `eng` matched case-insensitively to `ENG`.
- The Manager bonus override: 85,000 × 20% + 5,000 = 22,000.
- The record's generated `ToString()`.
- The `[audit]` line, printed by the CLI's event handler while `GiveRaise` is still running. That's why it
  appears before "New monthly salary".

### 3. Error handling

Input: `2`, `99`, then `4`, `MARKETING`, then `6`, `3`, `80`, then `7`, `2`, *(empty)*, `hr`, then `3`, `ar`, `0`

```
Employee ID: Error: Employee #99 was not found.

Department code: Error: Department 'MARKETING' was not found.

Employee ID: Raise %: Invalid input: Raise must be more than 0% and at most 50%.

Employee ID: New department code: Can't be empty. New department code: Transferred: #2 Priya (Full-time, HR, Remote)

Name contains: ID  Name      Role       Dept   Location   Monthly pay  Band
3   Arun      Intern     ENG    Hybrid       21,000.00  Junior
5   Kumar     Full-time  SALES  Office       60,000.00  Junior
```

Every error is a custom exception caught in `Program.cs`; the app keeps running. The rejected raise
fires no `[audit]` line, because validation throws before the event is raised.

### 4. Save and load, with the audit event

Starts with no `employees.json`. Input: `10`, then `6`, `2`, `10`, then `9`, then `6`, `2`, `5`, then `10`, then `1`, `0`

```
Error: No saved file yet. Choose 9 to save first.

Employee ID: Raise %: [audit] Priya (#2) salary 90,000.00 -> 99,000.00
New monthly salary: 99,000.00

Saved 5 employees to employees.json.

Employee ID: Raise %: [audit] Priya (#2) salary 99,000.00 -> 1,03,950.00
New monthly salary: 1,03,950.00

Loaded 5 employees from employees.json.

ID  Name      Role       Dept   Location   Monthly pay  Band
1   Ravi      Manager    ENG    Office     1,60,000.00  Senior
2   Priya     Full-time  ENG    Remote     1,01,000.00  Mid
3   Arun      Intern     ENG    Hybrid       21,000.00  Junior
4   Meena     Manager    HR     Office     1,20,000.00  Mid
5   Kumar     Full-time  SALES  Office       60,000.00  Junior
```

The second raise (to 1,03,950) happened after the save, so loading the file undoes it: Priya is back to
99,000 + 2,000 remote allowance = 1,01,000. Loading a file with bad content prints
`File error: employees.json does not contain valid employee data.` and keeps the current data.

## Value types and reference types used

| Value types (copied on assignment) | Where |
|---|---|
| `int` | `Id`, menu choices |
| `int?` (`Nullable<int>`) | `Employee.ManagerId`: the top manager has none |
| `decimal` | `MonthlySalary`, pay, bonus. Base-10, so no rounding errors with money |
| `bool` | `running` loop flag |
| `DateTime`, `TimeSpan` | `Employee.JoinedOn`, the 5-second file timeout |
| `CancellationToken` | A small struct handed to async methods; the `CancellationTokenSource` (a class) controls it |

| Reference types (the reference is copied; both point to the same object) | Where |
|---|---|
| `class`: `Employee`, `Department`, `EmployeeService`, `InMemoryRepository<T>`, `JsonEmployeeStore` | Entities and services |
| `record`: `Address`, `EmployeeSummary`, `EmployeeRecord` | Data compared by value |
| `string` | Names and codes. Immutable, so it behaves like a value in practice |
| `List<T>`, `Dictionary<TKey,TValue>` | Collections |
| Interfaces: `IRepository<T>`, `IWorkLocation`, `IEmployeeStore` | Abstractions |
| Delegates: `Func<Employee,bool>`, `EventHandler<SalaryChangedEventArgs>` | Filters and the event |

**Where this matters in the code:** `EmployeeService.GiveRaise` gets an `Employee` from the
repository and changes `MonthlySalary`. Because `Employee` is a reference type, that's the same object
stored in the dictionary, so the change is visible everywhere with no "save" step needed. If
`Employee` were a `struct`, the service would modify a copy and the stored employee wouldn't change.

## Lesson → code map

| Lesson | Where to look |
|---|---|
| 1.2 Top-level statements, namespaces | `Program.cs`; the project name note above |
| 1.3 `decimal`, `TryParse`, `const` | `Employee.MonthlySalary`, `AskInt`/`AskDecimal`, `EmployeeService.MaxRaisePercent` |
| 1.4 `switch` statement and switch expression | Menu in `Program.cs`, `SalaryBands.For`, role selection in `HireEmployee` |
| 1.6 `int?`, `string?`, `?.`, `??` | `Employee.ManagerId/Email`, `EmployeeService.Summarize`, `GetEmployee`, `GetDepartment` |
| 1.7 `StringBuilder`, `OrdinalIgnoreCase`, `IsNullOrWhiteSpace` | `BuildPayrollReport`, `Codes.Match`, `AskRequired` |
| 1.8 Custom exceptions, specific catches | `Exceptions/`, the `try/catch` in `Program.cs` (`FileNotFoundException` before `IOException`) |
| 2.1 Properties, `init`, `required`, primary constructor | `Employee`, `EmployeeService(...)`, `SalaryChangedEventArgs(...)`, `JsonEmployeeStore(...)` |
| 2.2 `internal` | `Services/Codes.cs` (invisible to the Cli project) |
| 2.3 `virtual`/`override`/`sealed` | `Employee.CalculateBonus`, `Manager`, `Intern` |
| 2.4 Abstract class + interfaces | `Employee`, `IRepository<T>`, `IWorkLocation`, `IEmployeeStore` |
| 2.5 Records | `Address`, `EmployeeSummary`, `EmployeeRecord` (storage DTO) |
| 2.6 SOLID, composition | DIP: repositories injected into `EmployeeService`, `IEmployeeStore` used by the CLI; Liskov: `Intern` bonus = 0; composition: `IWorkLocation` |
| 3.1 `Dictionary`, `List`, `TryGetValue` | `InMemoryRepository<T>` (and why `GetAll` sorts: a Dictionary has no guaranteed order), `HeadcountByDepartment` |
| 3.2 Generics + constraints | `IRepository<T> where T : class, IEntity` |
| 3.3 Delegates, `Func`, lambdas, closures, events | `EmployeeService.SalaryChanged` + `SalaryChangedEventArgs`; the `+=` subscriber in `Program.cs`; `EmployeeService.Find(Func<Employee,bool>)`; the closure over `trimmed` in `SearchByName` |
| 3.4 LINQ | `EmployeeService`: `GetDepartment` (`FirstOrDefault`), `NextEmployeeId` (`DefaultIfEmpty().Max()`), `GetByDepartment`, `SearchByName`/`Find` (`Where` + `ToList`), `HeadcountByDepartment` and `BuildPayrollReport` (`GroupBy` + `ToDictionary` + `Sum`); deferred execution test in `EmployeeExtensionsTests` |
| 3.5 Extension methods | `Services/EmployeeExtensions.cs`: `TotalMonthlyPay()`, `InDepartment(code)`; used by `BuildPayrollReport` and `GetByDepartment` |
| 3.6 Pattern matching | `Hire`: `employee is Intern { MonthlySalary: > MaxInternSalary }` (type + property + relational) and `employee is { ManagerId: int managerId }`; `EmployeeRecord.From` (switch on type) and `ToEmployee` (switch on string). `CalculateBonus` stays `virtual`: polymorphism, not a type switch |
| 4.1 `async`/`await` | `JsonEmployeeStore.SaveAsync/LoadAsync` (`File.WriteAllTextAsync`/`ReadAllTextAsync`); `Program.cs` awaits them from the menu loop (async `Main`, no `.Result`, no `async void`) |
| 4.2 Async traps avoided | `Task`-returning local functions, `CS4014` is an error via `TreatWarningsAsErrors`, tests are `async Task` |
| 4.3 Cancellation | `new CancellationTokenSource(fileTimeout)` in `SaveToFileAsync`/`LoadFromFileAsync`; `ct.ThrowIfCancellationRequested()` + passing `ct` down in `JsonEmployeeStore`; `catch (OperationCanceledException)` in `Program.cs`; temp-file-then-move so a cancelled save can't corrupt the old file |
| 4.4 xUnit | `tests/EmployeeApp.Tests`: AAA, `[Fact]` vs `[Theory]` + `[InlineData]` (raise boundaries, bonus per type), a fresh class instance per test, `IDisposable` cleanup of temp files, `Assert.ThrowsAnyAsync<OperationCanceledException>`, real `InMemoryRepository` as the test double (no mocking library) |
| 4.5 Tooling | `Directory.Build.props` (shared `TargetFramework`/`Nullable`/`ImplicitUsings`/`TreatWarningsAsErrors`); short `.csproj` files; NuGet `PackageReference`s in the test project; `launchSettings.json`; build/test/publish and Debug vs Release commands above |

## Gate 1 walkthrough picks

**Class: `Employee` (`Models/Employee.cs` + `Models/EmployeeTypes.cs`).** `Employee` is an abstract class:
it holds the state every employee shares (`Id`, `Name`, a validated `MonthlySalary` property) and declares
`abstract Role` and `virtual CalculateBonus()`. `Manager` overrides the bonus (20% + 5,000), and `Intern` is
`sealed` and returns 0. When code calls `CalculateBonus()` through an `Employee` variable, the runtime
type's version runs. That's polymorphism, and it's why there's no `switch` on type for bonuses. Where someone
works is composed in (`IWorkLocation`), not inherited, so changing location never means changing class.

**LINQ query: `EmployeeService.BuildPayrollReport`.** `staff.GroupBy(e => e.DepartmentCode)` buckets
employees by department. `.ToDictionary(g => g.Key, g => g.TotalMonthlyPay())` turns each bucket into
"code → total pay" in one pass, and `TotalMonthlyPay` is our extension method wrapping `Sum`. `ToDictionary`
runs the query immediately (no deferred execution), and `staff` is already a `List`, so enumerating it twice
does not repeat any work. The departments are then looped in order so empty ones still print.

**Async method: `JsonEmployeeStore.SaveAsync` (called from `SaveToFileAsync` in `Program.cs`).** The CLI
creates a `CancellationTokenSource` with a 5-second timeout and `await`s `SaveAsync`. That frees the thread
while the disk works, and the method continues where it left off once the write completes. Cancellation is
cooperative: `SaveAsync` checks `ct.ThrowIfCancellationRequested()` up front and passes `ct` into
`File.WriteAllTextAsync`, and a timeout surfaces as an `OperationCanceledException` that the menu loop
catches. It writes to a `.tmp` file and then moves it over the real one, so a cancelled save leaves the
previous file intact.
