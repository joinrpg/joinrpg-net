using System;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
  public static class WorldObjectExtensions
  {
    public static bool IsVisible([NotNull] this IWorldObject cg, int? currentUserId)
    {
      if (cg == null) throw new ArgumentNullException(nameof(cg));
      return cg.IsPublic || cg.Project.Details.PublishPlot || cg.HasMasterAccess(currentUserId);
    }
  }
}
