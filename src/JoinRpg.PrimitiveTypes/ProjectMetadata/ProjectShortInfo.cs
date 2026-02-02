namespace JoinRpg.PrimitiveTypes.ProjectMetadata;

/// <summary>
/// Проект без учета отношения к проекту для текущего пользователя
/// </summary>
public record ProjectShortInfo(
    ProjectIdentification ProjectId,
    ProjectLifecycleStatus ProjectLifecycleStatus,
    bool PublishPlot,
    ProjectName ProjectName,
    int ActiveClaimsCount,
    DateOnly LastUpdatedAt)
{
    public bool Active => ProjectLifecycleStatus != ProjectLifecycleStatus.Archived;

    public bool IsAcceptingClaims => ProjectLifecycleStatus == ProjectLifecycleStatus.ActiveClaimsOpen;
}
