using System.Runtime.CompilerServices;

namespace JoinRpg.PrimitiveTypes;
public interface IProjectEntityId
{
    ProjectIdentification ProjectId { get; }
    int Id { get; }
}

public static class IProjectEntityIdExtensions
{
    public static IReadOnlyCollection<TId> EnsureSameProject<TId>(
        this IReadOnlyCollection<TId> entityIds,
        [CallerArgumentExpression(nameof(entityIds))] string name = "entityId")
        where TId : IProjectEntityId
    {
        if (entityIds.Select(c => c.ProjectId).Distinct().Count() > 1)
        {
            throw new ArgumentException("Нельзя смешивать разные проекты в запросе!", name);
        }
        return entityIds;
    }
}
