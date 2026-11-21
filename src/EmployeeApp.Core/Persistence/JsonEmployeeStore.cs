using System.Text.Json;
using EmployeeApp.Core.Models;

namespace EmployeeApp.Core.Persistence;

/// <summary>
/// Saves and loads employees as a JSON file, using the async file APIs.
/// 4.1 <c>await</c> frees the thread while the disk works; the method resumes when the I/O finishes.
/// 4.3 Cancellation is cooperative: we check the token ourselves AND pass it to every async call.
/// </summary>
public sealed class JsonEmployeeStore(string filePath) : IEmployeeStore
{
    // static readonly: build the options once and reuse them (JsonSerializer caches per options instance).
    // The two Respect* flags make reading fail with JsonException when a non-nullable field such as
    // "Name" is null or missing, instead of quietly creating an employee with a null name (1.6).
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true
    };

    public string FilePath { get; } = filePath;

    public async Task SaveAsync(IEnumerable<Employee> employees, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(employees);
        ct.ThrowIfCancellationRequested();   // 4.3 check point: don't start if already cancelled

        // 3.4 ToList() runs the Select once, before any file work starts.
        List<EmployeeRecord> records = employees.Select(EmployeeRecord.From).ToList();
        string json = JsonSerializer.Serialize(records, JsonOptions);

        // Write to a temp file, then swap it in. If we're cancelled mid-write, the old file survives intact.
        string tempPath = FilePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, ct);   // 4.3 pass the token down
        File.Move(tempPath, FilePath, overwrite: true);
    }

    public async Task<IReadOnlyList<Employee>> LoadAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        string json = await File.ReadAllTextAsync(FilePath, ct);   // throws FileNotFoundException if never saved
        List<EmployeeRecord> records = JsonSerializer.Deserialize<List<EmployeeRecord>>(json, JsonOptions) ?? [];

        return records.Select(r => r.ToEmployee()).ToList();
    }
}
