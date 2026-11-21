namespace EmployeeApp.Core.Models;

public class Department : IEntity
{
    public int Id { get; init; }
    public required string Code { get; init; }   // short code like "ENG"; never changes
    public required string Name { get; set; }    // display name; can be renamed
}
