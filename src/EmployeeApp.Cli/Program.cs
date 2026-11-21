// 1.2 Top-level statements: the compiler generates Program.Main around this file.
// 4.1 Because this file uses await, the generated Main is `async Task Main`: no .Result, no async void.
using System.Text.Json;
using EmployeeApp.Core.Exceptions;
using EmployeeApp.Core.Models;
using EmployeeApp.Core.Persistence;
using EmployeeApp.Core.Repositories;
using EmployeeApp.Core.Services;

// 2.6 Dependency Inversion: the app chooses the concrete repositories and injects them.
var service = new EmployeeService(new InMemoryRepository<Employee>(), new InMemoryRepository<Department>());
SampleData.Seed(service);   // shared with the Web API (Week 2)

// 4.1 The save file sits next to the .exe (bin/Debug/net10.0/), so it's the same file whether the app
// is started with F5 in Visual Studio or `dotnet run` from any folder.
string savePath = Path.Combine(AppContext.BaseDirectory, "employees.json");
IEmployeeStore store = new JsonEmployeeStore(savePath);
TimeSpan fileTimeout = TimeSpan.FromSeconds(5);   // 4.3 give up on a stuck disk/network share after 5 s

// 3.3 Subscribe to the event with a lambda. The service raises it; the CLI decides what to do with it.
service.SalaryChanged += (_, e) =>
    Console.WriteLine($"[audit] {e.Employee.Name} (#{e.Employee.Id}) salary {e.OldSalary:N2} -> {e.NewSalary:N2}");

Console.WriteLine("=== Employee & Department Management ===");

bool running = true;
while (running)
{
    PrintMenu();
    string choice = Ask("Choose an option: ");

    try
    {
        // 1.4 switch statement: no fall-through, every case ends with break
        switch (choice)
        {
            case "1": ListEmployees(service.GetEmployees()); break;
            case "2": ShowEmployee(); break;
            case "3": ListEmployees(service.SearchByName(Ask("Name contains: "))); break;
            case "4": ListEmployees(service.GetByDepartment(Ask("Department code: "))); break;
            case "5": HireEmployee(); break;
            case "6": GiveRaise(); break;
            case "7": TransferEmployee(); break;
            case "8": Console.WriteLine(service.BuildPayrollReport()); break;
            case "9": await SaveToFileAsync(); break;     // 4.1 await: the loop waits for the save to finish
            case "10": await LoadFromFileAsync(); break;
            case "0": running = false; break;
            default: Console.WriteLine("Unknown option."); break;
        }
    }
    // 1.8 Catch specific exceptions first; each one becomes a friendly message instead of a crash
    catch (EmployeeNotFoundException ex) { Console.WriteLine($"Error: {ex.Message}"); }
    catch (DepartmentNotFoundException ex) { Console.WriteLine($"Error: {ex.Message}"); }
    catch (ValidationException ex) { Console.WriteLine($"Invalid input: {ex.Message}"); }
    catch (InvalidOperationException ex) { Console.WriteLine($"Not allowed: {ex.Message}"); }
    // 4.3 Thrown when the timeout's token fires (TaskCanceledException is a subclass, so it's caught too)
    catch (OperationCanceledException) { Console.WriteLine($"Cancelled: the file operation took longer than {fileTimeout.TotalSeconds} s."); }
    catch (FileNotFoundException) { Console.WriteLine("Error: No saved file yet. Choose 9 to save first."); }
    catch (IOException ex) { Console.WriteLine($"File error: {ex.Message}"); }   // after its subclass FileNotFoundException
    catch (JsonException) { Console.WriteLine("File error: employees.json does not contain valid employee data."); }

    Console.WriteLine();
}

Console.WriteLine("Goodbye!");

// ---------------------------------------------------------------------------
// Local functions (allowed after top-level statements)
// ---------------------------------------------------------------------------

void PrintMenu()
{
    Console.WriteLine("1. List employees        5. Hire employee");
    Console.WriteLine("2. Find employee by ID   6. Give raise");
    Console.WriteLine("3. Search by name        7. Transfer employee");
    Console.WriteLine("4. List a department     8. Payroll report");
    Console.WriteLine("9. Save to file          10. Load from file");
    Console.WriteLine("0. Exit");
}

void ListEmployees(IReadOnlyList<Employee> list)
{
    if (list.Count == 0)
    {
        Console.WriteLine("No employees found.");
        return;
    }

    Console.WriteLine($"{"ID",-4}{"Name",-10}{"Role",-11}{"Dept",-7}{"Location",-9}{"Monthly pay",13}  Band");
    foreach (var e in list)
    {
        var s = service.Summarize(e);
        Console.WriteLine($"{s.Id,-4}{s.Name,-10}{s.Role,-11}{s.Department,-7}{s.Location,-9}{s.MonthlyPay,13:N2}  {s.Band}");
    }
}

void ShowEmployee()
{
    int id = AskInt("Employee ID: ");
    var s = service.Summarize(service.GetEmployee(id));
    Console.WriteLine(s);   // 2.5 record ToString() prints every property
}

void HireEmployee()
{
    string name = AskRequired("Name: ");
    string dept = AskRequired("Department code: ");
    decimal salary = AskDecimal("Monthly salary: ");
    string role = Ask("Role (1 = Full-time, 2 = Manager, 3 = Intern): ");
    int id = service.NextEmployeeId();

    // 1.4 switch expression returning an object; 2.1 object initializers with required members
    Employee hire = role switch
    {
        "2" => new Manager { Id = id, Name = name, DepartmentCode = dept, MonthlySalary = salary },
        "3" => new Intern { Id = id, Name = name, DepartmentCode = dept, MonthlySalary = salary },
        _ => new FullTimeEmployee { Id = id, Name = name, DepartmentCode = dept, MonthlySalary = salary }
    };

    service.Hire(hire);
    Console.WriteLine($"Hired {hire}.");
}

void GiveRaise()
{
    int id = AskInt("Employee ID: ");
    decimal percent = AskDecimal("Raise %: ");
    decimal newSalary = service.GiveRaise(id, percent);   // the [audit] line prints from inside this call
    Console.WriteLine($"New monthly salary: {newSalary:N2}");
}

void TransferEmployee()
{
    int id = AskInt("Employee ID: ");
    string dept = AskRequired("New department code: ");
    service.Transfer(id, dept);
    Console.WriteLine($"Transferred: {service.GetEmployee(id)}");
}

// ---------- File (4.1–4.3) ----------

// 4.3 `using` disposes the CancellationTokenSource (it owns a timer). The token cancels itself after 5 s.
async Task SaveToFileAsync()
{
    using var cts = new CancellationTokenSource(fileTimeout);
    var staff = service.GetEmployees();
    await store.SaveAsync(staff, cts.Token);
    Console.WriteLine($"Saved {staff.Count} employees to {Path.GetFileName(savePath)}.");
}

async Task LoadFromFileAsync()
{
    using var cts = new CancellationTokenSource(fileTimeout);
    var loaded = await store.LoadAsync(cts.Token);
    service.ReplaceAllEmployees(loaded);
    Console.WriteLine($"Loaded {loaded.Count} employees from {Path.GetFileName(savePath)}.");
}

// ---------- Input helpers ----------

/// Reads one line. When input ends (e.g. piped input), exits cleanly instead of looping forever.
static string Ask(string prompt)
{
    Console.Write(prompt);
    string? line = Console.ReadLine();   // 1.6 ReadLine returns string? : null at end of input
    if (line is null)
    {
        Console.WriteLine();
        Console.WriteLine("Input ended. Goodbye!");
        Environment.Exit(0);
    }
    return line.Trim();
}

static string AskRequired(string prompt)
{
    string value = Ask(prompt);
    while (string.IsNullOrWhiteSpace(value))   // 1.7
        value = Ask($"Can't be empty. {prompt}");
    return value;
}

/// 1.3 TryParse instead of Parse: bad input is expected, not exceptional (1.8).
static int AskInt(string prompt)
{
    int value;
    while (!int.TryParse(Ask(prompt), out value))
        Console.WriteLine("Please enter a whole number.");
    return value;
}

static decimal AskDecimal(string prompt)
{
    decimal value;
    while (!decimal.TryParse(Ask(prompt), out value))
        Console.WriteLine("Please enter a number.");
    return value;
}
