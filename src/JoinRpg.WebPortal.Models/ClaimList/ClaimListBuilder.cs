using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Web.Claims;
using JoinRpg.Web.Claims.UnifiedGrid;
using JoinRpg.Web.Models.Claims;

namespace JoinRpg.Web.Models.ClaimList;

public static class ClaimListBuilder
{
    internal static ClaimListItemViewModel BuildItem(Claim claim, ICurrentUserAccessor currentUserId, ProjectInfo projectInfo,
       IClaimProblemValidator claimValidator, ClaimInfo problemContext, Dictionary<int, int> unreadComments)
    {
        var accessArguments = AccessArgumentsFactory.Create(claim, currentUserId, projectInfo);
        var balance = claim.CalculateClaimBalance(projectInfo);
        (DateTime lastModifiedAt, var lastModifiedBy) = GetLastComment(claim, accessArguments);

        return new ClaimListItemViewModel(
            claim.Character.CharacterName,
            new UserLinkViewModel(claim.Player.ToUserInfoHeader()),
            projectInfo.ProjectName,
            ClaimStatusBuilders.CreateFullStatus(claim, accessArguments),
            lastModifiedAt,
            claim.CreateDate,
            claim.CheckInDate,
            new UserLinkViewModel(projectInfo.GetMasterById(new UserIdentification(claim.ResponsibleMasterUserId)).UserInfo),
            FeePaid: balance.FeePaid,
            FeeDue: balance.FeeDue,
            TotalFee: balance.TotalFee,
            new UserLinkViewModel(lastModifiedBy),
            claim.GetId(),
            claimValidator.Validate(problemContext).Select(p => new ProblemViewModel(p)).ToList(),
            unreadComments.GetValueOrDefault(claim.CommentDiscussionId),
            claim.Player.FullName
            );
    }

    /// <param name="playerInfo">
    /// Профиль игрока. Приходит параметром, загруженный пачкой на весь список: раньше выгрузка
    /// читала паспорт и адрес прямо из <c>claim.Player.Extra</c>, то есть по ленивой навигации
    /// EF-сущности на каждую строку.
    /// </param>
    /// <param name="roomName">
    /// Комната группы заявки из плана поселения (ADR022), или <c>null</c>, если не расселена. Цепочка
    /// навигаций <c>AccommodationRequest.Accommodation</c> догружала бы комнату на каждую строку.
    /// </param>
    internal static ClaimListItemForExportViewModel BuildItemForExport(
        Claim claim, ICurrentUserAccessor currentUserId, ProjectInfo projectInfo, UserInfo playerInfo, string? roomName)
    {
        var accessArguments = AccessArgumentsFactory.Create(claim, currentUserId, projectInfo);
        (DateTime lastModifiedAt, var lastModifiedBy) = GetLastComment(claim, accessArguments);
        var balance = claim.CalculateClaimBalance(projectInfo);

        string? PassportData, RegistrationAddress;
        if (claim.PlayerAllowedSenstiveData && projectInfo.ProfileRequirementSettings.SensitiveDataRequired)
        {
            PassportData = playerInfo.PassportData;
            RegistrationAddress = playerInfo.RegistrationAddress;
        }
        else
        {
            PassportData = RegistrationAddress = null;
        }

        return new ClaimListItemForExportViewModel(
            claim.Character.CharacterName,
            new UserLinkViewModel(claim.Player.ToUserInfoHeader()),
            projectInfo.ProjectName,
            ClaimStatusBuilders.CreateFullStatus(claim, accessArguments),
            lastModifiedAt,
            claim.CreateDate,
            claim.CheckInDate,
            new UserLinkViewModel(projectInfo.GetMasterById(new UserIdentification(claim.ResponsibleMasterUserId)).UserInfo),
            FeePaid: balance.FeePaid,
            FeeDue: balance.FeeDue,
            TotalFee: balance.TotalFee,
            new UserLinkViewModel(lastModifiedBy),
            claim.GetId(),
            claim.GetAccommodationType(projectInfo)?.Name,
            roomName,
            claim.PreferentialFeeUser,
            PassportData,
            RegistrationAddress,
            claim.GetFields(projectInfo).ToDictionary(x => x.Field.Id, x => x.DisplayString),
            playerInfo
            );
    }

    public static (DateTime At, UserInfoHeader By) GetLastComment(Claim claim, AccessArguments accessArguments)
    {
        var lastComment = (At: claim.CreateDate, By: claim.Player);

        if (claim.LastPlayerCommentAt is not null && claim.LastPlayerCommentAt > lastComment.At)
        {
            lastComment = (At: claim.LastPlayerCommentAt.Value.DateTime, By: claim.Player);
        }

        if (claim.LastVisibleMasterCommentAt is not null && claim.LastVisibleMasterCommentAt > lastComment.At)
        {
            lastComment = (At: claim.LastVisibleMasterCommentAt.Value.DateTime, By: claim.LastVisibleMasterCommentBy!);
        }

        if (accessArguments.MasterAccess && claim.LastMasterCommentAt is not null && claim.LastMasterCommentAt > lastComment.At)
        {
            lastComment = (At: claim.LastMasterCommentAt.Value.DateTime, By: claim.LastMasterCommentBy!);
        }

        return (lastComment.At, lastComment.By.ToUserInfoHeader());
    }

    /// <summary>
    /// То же, что для EF-сущности, но поверх доменного агрегата (ADR013).
    /// </summary>
    public static DateTime GetLastCommentTime(CharacterClaimInfo claim, AccessArguments accessArguments)
    {
        var lastCommentDate = claim.CreateDate;

        if (claim.LastPlayerCommentAt is not null && claim.LastPlayerCommentAt > lastCommentDate)
        {
            lastCommentDate = claim.LastPlayerCommentAt.Value.DateTime;
        }

        if (claim.LastVisibleMasterCommentAt is not null && claim.LastVisibleMasterCommentAt > lastCommentDate)
        {
            lastCommentDate = claim.LastVisibleMasterCommentAt.Value.DateTime;
        }

        if (accessArguments.MasterAccess && claim.LastMasterCommentAt is not null && claim.LastMasterCommentAt > lastCommentDate)
        {
            lastCommentDate = claim.LastMasterCommentAt.Value.DateTime;
        }

        return lastCommentDate;
    }

    public static DateTime GetLastCommentTime(Claim claim, AccessArguments accessArguments)
    {
        var lastCommentDate = claim.CreateDate;

        if (claim.LastPlayerCommentAt is not null && claim.LastPlayerCommentAt > lastCommentDate)
        {
            lastCommentDate = claim.LastPlayerCommentAt.Value.DateTime;
        }

        if (claim.LastVisibleMasterCommentAt is not null && claim.LastVisibleMasterCommentAt > lastCommentDate)
        {
            lastCommentDate = claim.LastVisibleMasterCommentAt.Value.DateTime;
        }

        if (accessArguments.MasterAccess && claim.LastMasterCommentAt is not null && claim.LastMasterCommentAt > lastCommentDate)
        {
            lastCommentDate = claim.LastMasterCommentAt.Value.DateTime;
        }

        return lastCommentDate;
    }
}
