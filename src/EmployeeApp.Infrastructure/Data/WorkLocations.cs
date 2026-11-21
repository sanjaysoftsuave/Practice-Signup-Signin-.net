using EmployeeApp.Core.Models;

namespace EmployeeApp.Infrastructure.Data;

/// <summary>Turns the stored text ("Remote") back into the matching <see cref="IWorkLocation"/> object.</summary>
public static class WorkLocations
{
    public static IWorkLocation FromName(string name) => name switch
    {
        "Remote" => new Remote(),
        "Hybrid" => new Hybrid(),
        "Office" => new Office(),
        _ => throw new InvalidOperationException($"Unknown work location '{name}' in the database.")
    };
}
