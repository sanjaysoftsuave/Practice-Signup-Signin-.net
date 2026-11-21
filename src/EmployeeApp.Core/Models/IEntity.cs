namespace EmployeeApp.Core.Models;

/// <summary>
/// Anything stored in a repository. Used as a generic constraint (3.2): <c>where T : IEntity</c>.
/// </summary>
public interface IEntity
{
    int Id { get; }
}
