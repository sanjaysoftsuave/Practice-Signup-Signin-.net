namespace EmployeeApp.Core.Services;

/// <summary>
/// 2.2 internal: a helper for this assembly only. EmployeeApp.Cli can't see it.
/// </summary>
internal static class Codes
{
    /// <summary>1.7 Case-insensitive compare without ToLower(): "eng", "ENG", " Eng " all match.</summary>
    internal static bool Match(string a, string b) =>
        string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}
