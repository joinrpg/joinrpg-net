namespace JoinRpg.PrimitiveTypes.ProjectMetadata;

public record ProjectShortInfo(
    ProjectIdentification ProjectId,
    ProjectLifecycleStatus ProjectLifecycleStatus,
    bool PublishPlot,
    ProjectName ProjectName,
    int ActiveClaimsCount,
    bool HasMyClaims,
    bool HasMyMasterAccess)
{
    public bool Active => ProjectLifecycleStatus != ProjectLifecycleStatus.Archived;

    public bool IsAcceptingClaims => ProjectLifecycleStatus == ProjectLifecycleStatus.ActiveClaimsOpen;
}

public static class ProjectShortInfoSorter
{
    public static IOrderedEnumerable<ProjectShortInfo> OrderByDisplayPriority(this IEnumerable<ProjectShortInfo> projectShortInfos)
        => projectShortInfos
            .OrderByDescending(p => p.Active)
            .ThenByDescending(p => p.HasMyMasterAccess)
            .ThenByDescending(p => p.HasMyClaims)
            .ThenByDescending(p => p.ActiveClaimsCount);
}
