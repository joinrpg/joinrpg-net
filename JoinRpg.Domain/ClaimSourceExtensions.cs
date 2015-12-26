 using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain
{
  public static class ClaimSourceExtensions
  {
    public static bool HasClaimForUser<T>(this T claimSource, int currentUserId) where T : IClaimSource
    {
      return claimSource.Claims.Any(c => c.PlayerUserId == currentUserId && c.IsActive);
    }

    public static IEnumerable<User> GetResponsibleMasters(this IClaimSource @group, bool includeSelf = true)
    {
      if (group.ResponsibleMasterUser != null && includeSelf)
      {
        yield return group.ResponsibleMasterUser;
        yield break;
      }
      var directParents = group.ParentGroups.SelectMany(g => g.GetResponsibleMasters()).WhereNotNull().Distinct();
      foreach (var directParent in directParents)
      {
        yield return directParent;
      }
    }

    [NotNull]
    public static IEnumerable<CharacterGroup> GetParentGroups([CanBeNull] this IClaimSource @group)
    {
      return @group.FlatTree(g => g.ParentGroups, false).Cast<CharacterGroup>();
    }

    public static bool HasActiveClaims(this IClaimSource characterGroup)
    {
      return characterGroup.Claims.Any(claim => claim.IsActive);
    }
  }
}
