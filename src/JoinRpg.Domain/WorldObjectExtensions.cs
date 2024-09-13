using JoinRpg.DataModel;

namespace JoinRpg.Domain;

public static class WorldObjectExtensions
{
    public static bool IsVisible(this IWorldObject cg, int? currentUserId)
    {
        ArgumentNullException.ThrowIfNull(cg);

        return cg.IsPublic || cg.Project.Details.PublishPlot || cg.HasMasterAccess(currentUserId);
    }
}
