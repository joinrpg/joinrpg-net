using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Claims;
using JoinRpg.Web.Claims.UnifiedGrid;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.Web.Models.UserProfile;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebComponents;

namespace JoinRpg.WebPortal.Managers.UnifiedGrid;
public static class ItemBuilder
{
    public static UgItemForCaptainViewModel? BuildItemForCaptain(UgDto ugItem, ICurrentUserAccessor currentUserId, ProjectInfo projectInfo)
    {
        var accessArguments = AccessArgumentsFactory.Create(ugItem, currentUserId, projectInfo);

        if (!accessArguments.CanViewCharacterName)
        {
            return null;
        }

        var character = new UgCharacterForCaptainViewModel(
           new CharacterLinkSlimViewModel(ugItem.CharacterId, ugItem.CharacterName, ugItem.IsActive, ViewModeSelector.Create(ugItem.IsPublic, accessArguments.CanViewCharacterName)),
           ugItem.GetBusyStatus(),
           ugItem.SlotCount,
           [] // TODO
           );

        return new UgItemForCaptainViewModel(
            character,
            [.. ugItem.Claims.Select(x => BuildClaimItem(x, projectInfo, accessArguments))]
            );
    }

    private static UgClaimForCaptainViewModel BuildClaimItem(UgClaim ugClaim, ProjectInfo projectInfo, PrimitiveTypes.Access.AccessArguments accessArguments)
    {
        var claim = ugClaim.Claim;
        var lastModifiedAt = ClaimListBuilder.GetLastCommentTime(claim, accessArguments);

        var respMaster = projectInfo.Masters.Single(x => x.UserId == claim.ResponsibleMasterUserId).UserInfo;

        return new UgClaimForCaptainViewModel(
            UserLinks.Create(claim.Player, ViewMode.Show),
            (ClaimStatusView)claim.ClaimStatus,
            lastModifiedAt,
            claim.CreateDate,
            claim.CheckInDate,
            UserLinks.Create(respMaster, ViewMode.Show),
            ugClaim.CalculateClaimBalance(projectInfo),
            claim.GetId(),
            claim.Player.FullName
            );
    }
}
