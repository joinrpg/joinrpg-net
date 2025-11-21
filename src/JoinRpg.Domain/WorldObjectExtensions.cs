namespace JoinRpg.Domain;

public static class WorldObjectExtensions
{
    [Obsolete("Use AccessArguments & ProjectInfo")]
    public static bool IsVisible(this IWorldObject cg, int? currentUserId)
    {
        ArgumentNullException.ThrowIfNull(cg);

        return cg.IsPublic || cg.Project.Details.PublishPlot || cg.HasMasterAccess(currentUserId);
    }
}
