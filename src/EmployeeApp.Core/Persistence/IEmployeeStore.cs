using EmployeeApp.Core.Models;

namespace EmployeeApp.Core.Persistence;

/// <summary>
/// 2.6 Dependency Inversion again: the CLI talks to this abstraction, so the JSON file could be
/// swapped for a database later. 4.1 Both methods return <see cref="Task"/> because file I/O is slow
/// and waiting for it shouldn't block a thread. 4.3 The <see cref="CancellationToken"/> is the last
/// parameter, by convention.
/// </summary>
public interface IEmployeeStore
{
    Task SaveAsync(IEnumerable<Employee> employees, CancellationToken ct = default);

    Task<IReadOnlyList<Employee>> LoadAsync(CancellationToken ct = default);
}
