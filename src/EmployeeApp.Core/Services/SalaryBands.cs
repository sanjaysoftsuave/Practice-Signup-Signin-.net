namespace EmployeeApp.Core.Services;

public static class SalaryBands
{
    /// <summary>1.4 Switch expression with relational patterns; arms are checked top to bottom.</summary>
    public static string For(decimal monthlySalary) => monthlySalary switch
    {
        >= 150000m => "Senior",
        >= 75000m => "Mid",
        > 0m => "Junior",
        _ => "Unpaid"
    };
}
