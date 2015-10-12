using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain
{
  public static class ClaimExtensions
  {
    public static bool HasAccess(this Claim claim, int? currentUserId)
    {
      return currentUserId != null &&
             (claim.PlayerUserId == currentUserId || claim.Project.HasAccess(currentUserId.Value));
    }

    public static IEnumerable<Claim> OtherClaimsForThisPlayer(this Claim claim)
    {
      return claim.Player.Claims.Where(c => c.ClaimId != claim.ClaimId && c.IsActive && c.ProjectId == claim.ProjectId);
    }

    /// <summary>
    /// If claims for group, not for character, this is 0
    /// </summary>
    /// <param name="claim"></param>
    /// <returns></returns>
    [NotNull]
    public static IEnumerable<Claim> OtherClaimsForThisCharacter(this Claim claim)
    {
      return claim.Character?.Claims?.Where(c => c.PlayerUserId != claim.PlayerUserId && c.IsActive) ?? new List<Claim>();
    }

    public static IClaimSource GetTarget(this Claim claim)
    {
      return (IClaimSource) claim.Character ?? claim.Group;
    }

    public static IEnumerable<CharacterGroup> GetParentGroups(this Claim claim)
    {
      return claim.GetTarget() //Character or group
        .GetParentGroups() //Get parents
        .Union(claim.Group) //Don't forget group himself
        .WhereNotNull();
    }

    public static bool PartOfGroup(this Claim cl, int characterGroupId)
    {
      //TODO we can do faster than this
      return cl.GetParentGroups().Any(g => g.CharacterGroupId == characterGroupId);
    }
  }
}