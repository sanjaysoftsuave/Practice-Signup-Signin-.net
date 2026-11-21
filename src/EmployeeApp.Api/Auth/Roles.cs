namespace EmployeeApp.Api.Auth;

/// <summary>10.3 Role names and policy names in one place, so a typo is a compile error, not a silent 403.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}

public static class Policies
{
    /// <summary>Only an Admin may hire, update or fire. Used as <c>[Authorize(Policy = Policies.AdminOnly)]</c>.</summary>
    public const string AdminOnly = "AdminOnly";
}
