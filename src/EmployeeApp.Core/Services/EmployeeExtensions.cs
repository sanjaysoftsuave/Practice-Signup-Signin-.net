using EmployeeApp.Core.Models;

namespace EmployeeApp.Core.Services;

/// <summary>
/// 3.5 Extension methods: a static class + a static method with <c>this</c> on the first parameter.
/// They read like instance methods (<c>staff.TotalMonthlyPay()</c>) but are just static calls the
/// compiler rewrites to <c>EmployeeExtensions.TotalMonthlyPay(staff)</c>. That is exactly how LINQ's
/// <c>Where</c>/<c>Sum</c> are built on top of <see cref="IEnumerable{T}"/>.
/// </summary>
public static class EmployeeExtensions
{
    /// <summary>Sum of salary + location allowance. 0 for an empty sequence (Sum never throws on empty).</summary>
    public static decimal TotalMonthlyPay(this IEnumerable<Employee> staff) =>
        staff.Sum(e => e.MonthlyPay());

    /// <summary>
    /// 3.4 Deferred: returns a query, not a list. Nothing runs until the caller enumerates it
    /// (foreach, ToList, Count…). Matches codes case-insensitively ("eng" == "ENG").
    /// </summary>
    public static IEnumerable<Employee> InDepartment(this IEnumerable<Employee> staff, string code) =>
        staff.Where(e => Codes.Match(e.DepartmentCode, code));
}
