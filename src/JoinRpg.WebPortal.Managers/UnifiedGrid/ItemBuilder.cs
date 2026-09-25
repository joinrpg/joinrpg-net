using JoinRpg.Common.WebComponents;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Web.Claims.UnifiedGrid;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.Web.Models.Claims;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Managers.UnifiedGrid;

public static class ItemBuilder
{
    /// <param name="claims">
    /// Заявки, которые надо показать: агрегат несёт их все, а грид показывает отобранные фильтром.
    /// </param>
    public static UgItemForCaptainViewModel? BuildItemForCaptain(
        CharacterInfo character,
        IReadOnlyCollection<CharacterClaimInfo> claims,
        ICurrentUserAccessor currentUserId,
        ProjectInfo projectInfo)
    {
        var accessArguments = AccessArgumentsFactory.Create(character, currentUserId) with { IsCapitan = true };

        if (!accessArguments.CanViewCharacterName)
        {
            return null;
        }

        var characterViewModel = new UgCharacterForCaptainViewModel(
           new CharacterLinkSlimViewModel(
               character.Id, character.CharacterName, character.IsActive, ViewModeSelector.Create(character.IsPublic, accessArguments.CanViewCharacterName)),
           new CharacterApplyViewModel(
               character.Id,
               character.GetBusyStatus(),
               character.CharacterTypeInfo.SlotLimit,
               character.CharacterTypeInfo.IsHot,
               ClaimValidator.IsAvailableForPlayer(character)),
           [] // TODO
           );

        return new UgItemForCaptainViewModel(
            characterViewModel,
            [.. claims.Select(claim => BuildClaimItem(character, claim, projectInfo, accessArguments))]
            );
    }

    private static UgClaimForCaptainViewModel BuildClaimItem(
        CharacterInfo character,
        CharacterClaimInfo claim,
        ProjectInfo projectInfo,
        AccessArguments accessArguments)
    {
        var lastModifiedAt = ClaimListBuilder.GetLastCommentTime(claim, accessArguments);

        var respMaster = projectInfo.GetMasterById(claim.ResponsibleMasterId).UserInfo;

        return new UgClaimForCaptainViewModel(
            new UserLinkViewModel(claim.Player),
            ClaimStatusBuilders.CreateFullStatus(claim, accessArguments),
            lastModifiedAt,
            claim.CreateDate,
            claim.CheckInDate,
            new UserLinkViewModel(respMaster),
            character.CalculateClaimBalance(claim, projectInfo),
            claim.ClaimId,
            claim.Player.DisplayName.FullName
            );
    }
}
