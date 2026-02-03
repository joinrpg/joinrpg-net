namespace JoinRpg.PrimitiveTypes.ProjectMetadata;

/// <summary>
/// Проект + отношение к проекту для текущего пользователя
/// </summary>
public record ProjectPersonalizedInfo(
    ProjectIdentification ProjectId,
    ProjectLifecycleStatus ProjectLifecycleStatus,
    bool PublishPlot,
    ProjectName ProjectName,
    int ActiveClaimsCount,
    bool HasMyClaims,
    bool HasMyMasterAccess,
    KogdaIgraIdentification? LastKogdaIgraId)
{
    public bool Active => ProjectLifecycleStatus != ProjectLifecycleStatus.Archived;

    public bool IsAcceptingClaims => ProjectLifecycleStatus == ProjectLifecycleStatus.ActiveClaimsOpen;
}

public static class ProjectShortInfoSorter
{
    public static IOrderedEnumerable<ProjectPersonalizedInfo> OrderByDisplayPriority(this IEnumerable<ProjectPersonalizedInfo> projectShortInfos)
        => projectShortInfos
            .OrderByDescending(p => p.Active)
            .ThenByDescending(p => p.HasMyMasterAccess)
            .ThenByDescending(p => p.HasMyClaims)
            .ThenByDescending(p => p.ActiveClaimsCount);
}
