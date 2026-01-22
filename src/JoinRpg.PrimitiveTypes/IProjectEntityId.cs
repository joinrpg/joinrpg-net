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

    public static (ProjectIdentification projectId, IReadOnlyCollection<int> list)
        ToIntListSameProject<TId>(
    this IReadOnlyCollection<TId> entityIds,
    [CallerArgumentExpression(nameof(entityIds))] string name = "entityId")
    where TId : IProjectEntityId
    {
        if (entityIds.Count == 0)
        {
            throw new ArgumentException("Ожидается не пустой список", nameof(entityIds));
        }
        var list = new List<int>(entityIds.Count);
        var projectId = entityIds.First().ProjectId;
        foreach (var entityId in entityIds)
        {
            if (entityId.ProjectId != projectId)
            {
                throw new ArgumentException("Нельзя смешивать разные проекты в запросе!", name);
            }
            list.Add(entityId.Id);
        }
        return (projectId, list);
    }
}
