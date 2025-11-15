using System.Data.Entity.SqlServer;
using JoinRpg.PrimitiveTypes.Claims;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

internal static class CharacterPredicates
{
    internal static Expression<Func<Character, bool>> Hot() => character => character.IsHot && character.IsActive && character.ApprovedClaim == null;

    internal static Expression<Func<Character, bool>> ByGroupImprecise(IReadOnlyCollection<CharacterGroupIdentification> groups)
    {
        if (groups.Count == 0)
        {
            return character => false;
        }

        var projectId = groups.EnsureSameProject().First().ProjectId;
        var groupIntIds = groups.Select(x => x.CharacterGroupId).ToArray();

        return character => character.ProjectId == projectId
            && groupIntIds.Any(id => SqlFunctions.CharIndex(id.ToString(), character.ParentGroupsImpl.ListIds) > 0);
    }

    internal static Func<Character, bool> ByGroupPrecise(IReadOnlyCollection<CharacterGroupIdentification> groups)
    {
        if (groups.Count == 0)
        {
            return character => false;
        }

        var projectId = groups.EnsureSameProject().First().ProjectId;
        var groupIntIds = groups.Select(x => x.CharacterGroupId).ToArray();

        return character => character.ProjectId == projectId && groupIntIds.Intersect(character.ParentCharacterGroupIds).Any();
    }

    internal static Expression<Func<Character, bool>> ByUgStatus(UgStatusSpec spec)
    {
        var activeClaims = ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active);
        var inactiveClaims = ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.InActive);
        return spec switch
        {
            UgStatusSpec.Active => character => character.IsActive,
            UgStatusSpec.Vacant => character => character.IsActive && character.ApprovedClaimId == null && character.CharacterType != CharacterType.NonPlayer,
            UgStatusSpec.Discussion => character => character.IsActive && character.ApprovedClaimId == null && character.Claims.Any(x => activeClaims.Invoke(x)),
            UgStatusSpec.Archive => character => !character.IsActive && character.Claims.Any(x => inactiveClaims.Invoke(x)),
            _ => throw new NotImplementedException(),
        };
    }


}
