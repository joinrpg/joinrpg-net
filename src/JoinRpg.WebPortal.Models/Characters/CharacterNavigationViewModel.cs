using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Models.ClaimList;

namespace JoinRpg.Web.Models.Characters;

/// <summary>
/// TODO: LEO describe the meaning of this tricky class properly
/// </summary>
public class CharacterNavigationViewModel(Character character, int? currentUserId, ProjectInfo projectInfo)
{
    public CharacterNavigationPage Page { get; private set; }
    private AccessArguments AccessArguments { get; } = AccessArgumentsFactory.Create(character, UserIdentification.FromOptional(currentUserId), projectInfo);
    public bool HasMasterAccess => AccessArguments.MasterAccess;
    public bool CanEditRoles { get; } = character.HasEditRolesAccess(currentUserId);

    public bool CanAddClaim { get; private set; }
    public int? ClaimId { get; private set; }
    public int CharacterId { get; } = character.CharacterId;
    public int ProjectId { get; } = character.ProjectId;

    public string Name { get; } = character.CharacterName;

    public bool IsActive { get; } = character.IsActive;

    public IEnumerable<ClaimShortListItemViewModel> DiscussedClaims { get; } = LoadClaimsWithCondition(character, currentUserId, claim => claim.IsInDiscussion);
    public IEnumerable<ClaimShortListItemViewModel> RejectedClaims { get; } = LoadClaimsWithCondition(character, currentUserId, claim => !claim.ClaimStatus.IsActive());

    public static CharacterNavigationViewModel FromCharacter(Character character,
        CharacterNavigationPage page,
        int? currentUserId, ProjectInfo projectInfo)
    {
        int? claimId;

        if (character.ApprovedClaim?.HasAccess(currentUserId, Permission.None, ExtraAccessReason.Player) == true
        ) //If Approved Claim exists and we have access to it, so be it.
        {
            claimId = character.ApprovedClaim.ClaimId;
        }
        else // if we have My claims, try select single one. We may fail to do so.
        {
            claimId = character.Claims.Where(c => c.PlayerUserId == currentUserId).ToList()
                .TrySelectSingleClaim()?.ClaimId;
        }

        var vm = new CharacterNavigationViewModel(character, currentUserId, projectInfo)
        {
            CanAddClaim = character.IsAcceptingClaims(projectInfo),
            ClaimId = claimId,
            Page = page,
        };

        return vm;
    }

    private static IEnumerable<ClaimShortListItemViewModel> LoadClaimsWithCondition(Character field, int? currentUserId,
        Func<Claim, bool> predicate)
    {
        return field.HasMasterAccess(currentUserId)
            ? field.Claims.Where(predicate).Select(claim => new ClaimShortListItemViewModel(claim))
            : [];
    }

    public static CharacterNavigationViewModel FromClaim(
        Claim claim,
        int currentUserId,
        CharacterNavigationPage characterNavigationPage, ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(claim);

        var vm = new CharacterNavigationViewModel(claim.Character, currentUserId, projectInfo)
        {
            CanAddClaim = false,
            ClaimId = claim.ClaimId,
            Page = characterNavigationPage,
        };
        if (vm.RejectedClaims.Any(c => c.ClaimId == claim.ClaimId))
        {
            vm.Page = CharacterNavigationPage.RejectedClaim;
            vm.ClaimId = null;
        }

        return vm;
    }
}
