using System.ComponentModel;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Models.ClaimList;

namespace JoinRpg.Web.Models.Characters;

/// <summary>
/// TODO: LEO describe the meaning of this tricky class properly
/// </summary>
public class CharacterNavigationViewModel
{
    public CharacterNavigationPage Page { get; private set; }
    public bool HasMasterAccess { get; private set; }
    public bool CanEditRoles { get; private set; }

    public bool CanAddClaim { get; private set; }
    public int? ClaimId { get; private set; }
    public int? CharacterId { get; private set; }
    public int ProjectId { get; private set; }

    public string Name { get; private set; }

    [ReadOnly(true)]
    public bool IsActive { get; private set; }

    public IEnumerable<ClaimShortListItemViewModel> DiscussedClaims { get; private set; }
    public IEnumerable<ClaimShortListItemViewModel> RejectedClaims { get; private set; }

    public static CharacterNavigationViewModel FromCharacter(Character character,
        CharacterNavigationPage page,
        int? currentUserId)
    {
        int? claimId;

        if (character.ApprovedClaim?.HasAccess(currentUserId, ExtraAccessReason.Player) == true
        ) //If Approved Claim exists and we have access to it, so be it.
        {
            claimId = character.ApprovedClaim.ClaimId;
        }
        else // if we have My claims, try select single one. We may fail to do so.
        {
            claimId = character.Claims.Where(c => c.PlayerUserId == currentUserId)
                .TrySelectSingleClaim()?.ClaimId;
        }

        var vm = new CharacterNavigationViewModel
        {
            CanAddClaim = character.IsAvailable,
            ClaimId = claimId,
            HasMasterAccess = character.HasMasterAccess(currentUserId),
            CanEditRoles = character.HasEditRolesAccess(currentUserId),
            CharacterId = character.CharacterId,
            ProjectId = character.ProjectId,
            Page = page,
            Name = character.CharacterName,
            IsActive = character.IsActive,
        };

        vm.LoadClaims(character);
        return vm;
    }

    private void LoadClaims(Character? field)
    {
        RejectedClaims = LoadClaimsWithCondition(field, claim => !claim.ClaimStatus.IsActive());
        DiscussedClaims = LoadClaimsWithCondition(field, claim => claim.IsInDiscussion);
    }

    private IEnumerable<ClaimShortListItemViewModel> LoadClaimsWithCondition(Character? field,
        Func<Claim, bool> predicate)
    {
        return HasMasterAccess && field != null
            ? field.Claims.Where(predicate)
                .Select(claim => new ClaimShortListItemViewModel(claim))
            : Enumerable.Empty<ClaimShortListItemViewModel>();
    }

    public static CharacterNavigationViewModel FromClaim([NotNull]
        Claim claim,
        int currentUserId,
        CharacterNavigationPage characterNavigationPage)
    {
        if (claim == null)
        {
            throw new ArgumentNullException(nameof(claim));
        }

        var vm = new CharacterNavigationViewModel
        {
            CanAddClaim = false,
            ClaimId = claim.ClaimId,
            HasMasterAccess = claim.HasMasterAccess(currentUserId),
            CharacterId = claim.Character?.CharacterId,
            ProjectId = claim.ProjectId,
            Page = characterNavigationPage,
            Name = claim.GetTarget().Name,
            CanEditRoles = claim.HasEditRolesAccess(currentUserId),
            IsActive = claim.GetTarget().IsActive,
        };
        vm.LoadClaims(claim.Character);
        if (vm.RejectedClaims.Any(c => c.ClaimId == claim.ClaimId))
        {
            vm.Page = CharacterNavigationPage.RejectedClaim;
            vm.ClaimId = null;
        }

        return vm;
    }
}
