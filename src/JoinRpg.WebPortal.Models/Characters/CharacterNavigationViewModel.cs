using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.Web.Models.UserProfile;

namespace JoinRpg.Web.Models.Characters;

/// <summary>
/// Шапка страниц персонажа и заявки: кто это, что с ним можно сделать и на какую из заявок
/// переключиться.
/// </summary>
public class CharacterNavigationViewModel
{
    private readonly CharacterInfo character;

    private CharacterNavigationViewModel(CharacterInfo character, UserIdentification? currentUserId)
    {
        this.character = character;
        var projectInfo = character.ProjectInfo;

        AccessArguments = AccessArgumentsFactory.Create(character, currentUserId);
        CanEditRoles = projectInfo.HasEditRolesAccess(currentUserId);
        CanManageClaims = projectInfo.HasMasterAccess(currentUserId, Permission.CanManageClaims);
        CharacterId = character.Id.CharacterId;
        ProjectId = character.Id.ProjectId.Value;
        Name = character.CharacterName;
        IsActive = character.IsActive;

        DiscussedClaims = SelectClaims(claim => claim.IsInDiscussion, currentUserId);
        RejectedClaims = SelectClaims(claim => !claim.IsActive, currentUserId);
    }

    public CharacterNavigationPage Page { get; private set; }
    private AccessArguments AccessArguments { get; }
    public bool HasMasterAccess => AccessArguments.MasterAccess;
    public bool CanEditRoles { get; }
    public bool CanManageClaims { get; }

    public bool CanAddClaim { get; private set; }
    public int? ClaimId { get; private set; }
    public int CharacterId { get; }
    public int ProjectId { get; }

    public string Name { get; }

    public bool IsActive { get; }

    public IEnumerable<ClaimShortListItemViewModel> DiscussedClaims { get; }
    public IEnumerable<ClaimShortListItemViewModel> RejectedClaims { get; }

    public static CharacterNavigationViewModel FromCharacter(
        CharacterInfo character,
        CharacterNavigationPage page,
        UserIdentification? currentUserId)
    {
        ArgumentNullException.ThrowIfNull(character);

        return new CharacterNavigationViewModel(character, currentUserId)
        {
            // Показ считается глазами игрока, кто бы страницу ни открыл: мастерские послабления
            // здесь дали бы «заявиться можно» в проекте с закрытым приёмом заявок.
            CanAddClaim = ClaimValidator
                .Validate(character, userInfo: null, movedClaim: null, character.ProjectInfo, ClaimOperation.DisplayForPlayer)
                .Count == 0,
            ClaimId = SelectClaimToShow(character, currentUserId)?.ClaimId.ClaimId,
            Page = page,
        };
    }

    public static CharacterNavigationViewModel FromClaim(
        CharacterInfo character,
        ClaimIdentification claimId,
        UserIdentification currentUserId,
        CharacterNavigationPage characterNavigationPage)
    {
        ArgumentNullException.ThrowIfNull(character);

        var vm = new CharacterNavigationViewModel(character, currentUserId)
        {
            CanAddClaim = false,
            ClaimId = claimId.ClaimId,
            Page = characterNavigationPage,
        };

        if (vm.RejectedClaims.Any(c => c.ClaimId == claimId.ClaimId))
        {
            vm.Page = CharacterNavigationPage.RejectedClaim;
            vm.ClaimId = null;
        }

        return vm;
    }

    /// <summary>
    /// На какую заявку вести с карточки персонажа: на утверждённую, если она видна этому
    /// пользователю, иначе на его собственную — если её удаётся выбрать однозначно.
    /// </summary>
    private static CharacterClaimInfo? SelectClaimToShow(CharacterInfo character, UserIdentification? currentUserId)
    {
        if (currentUserId is null)
        {
            return null;
        }

        if (character.ApprovedClaim is { } approvedClaim && HasAccessToClaim(approvedClaim, character, currentUserId))
        {
            return approvedClaim;
        }

        return character.Claims.Where(c => c.PlayerId == currentUserId).ToList().TrySelectSingleClaim();
    }

    /// <summary>
    /// Эквивалент <c>ClaimAcccessExtensions.HasAccess(claim, userId, Permission.None,
    /// ExtraAccessReason.Player)</c> для агрегата: свою заявку игрок видит всегда, чужую — только
    /// с мастерским доступом.
    /// </summary>
    private static bool HasAccessToClaim(CharacterClaimInfo claim, CharacterInfo character, UserIdentification currentUserId)
        => claim.PlayerId == currentUserId || character.ProjectInfo.HasMasterAccess(currentUserId);

    private IEnumerable<ClaimShortListItemViewModel> SelectClaims(
        Func<CharacterClaimInfo, bool> predicate,
        UserIdentification? currentUserId)
        => character.ProjectInfo.HasMasterAccess(currentUserId)
            ? [.. character.Claims.Where(predicate).Select(claim =>
                new ClaimShortListItemViewModel(character.CharacterName, claim.ClaimId, UserLinks.Create(claim.Player)))]
            : [];
}
