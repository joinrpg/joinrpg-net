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
    /// <param name="claim">
    /// EF-заявка. Из неё читается только то, чего нет в агрегате (ADR021): последний комментарий
    /// вместе с автором и обсуждение — для счётчика непрочитанного.
    /// </param>
    /// <param name="claimInfo">Та же заявка как доменный тип — всё остальное берётся отсюда.</param>
    internal static ClaimListItemViewModel BuildItem(Claim claim, ICurrentUserAccessor currentUserId,
       IClaimProblemValidator claimValidator, ClaimInfo claimInfo, Dictionary<int, int> unreadComments)
    {
        var accessArguments = AccessArgumentsFactory.Create(claimInfo, currentUserId.UserIdentificationOrDefault);
        var balance = claimInfo.ClaimInCharacter.CalculateBalance();
        (DateTime lastModifiedAt, var lastModifiedBy) = GetLastComment(claim, accessArguments);
        var characterClaim = claimInfo.Claim;

        return new ClaimListItemViewModel(
            claimInfo.Character.CharacterName,
            new UserLinkViewModel(claimInfo.Player.ToUserInfoHeader()),
            claimInfo.ProjectInfo.ProjectName,
            ClaimStatusBuilders.CreateFullStatus(characterClaim, accessArguments),
            lastModifiedAt,
            characterClaim.CreateDate,
            characterClaim.CheckInDate,
            new UserLinkViewModel(claimInfo.ProjectInfo.GetMasterById(characterClaim.ResponsibleMasterId).UserInfo),
            FeePaid: balance.FeePaid,
            FeeDue: balance.FeeDue,
            TotalFee: balance.TotalFee,
            new UserLinkViewModel(lastModifiedBy),
            claimInfo.ClaimId,
            claimValidator.Validate(claimInfo).Select(p => new ProblemViewModel(p)).ToList(),
            unreadComments.GetValueOrDefault(claimInfo.Claim.CommentDiscussionId.Value),
            GetPlayerFullName(claimInfo)
            );
    }

    /// <param name="claim">
    /// EF-заявка. Из неё читается только то, чего нет в агрегате (ADR021): последний комментарий
    /// вместе с автором.
    /// </param>
    /// <param name="claimInfo">
    /// Та же заявка как доменный тип, загруженная пачкой на весь список. Профиль игрока тоже отсюда:
    /// раньше выгрузка читала паспорт и адрес прямо из <c>claim.Player.Extra</c>, то есть по ленивой
    /// навигации EF-сущности на каждую строку.
    /// </param>
    /// <param name="roomName">
    /// Комната группы заявки из плана поселения (ADR022), или <c>null</c>, если не расселена. Цепочка
    /// навигаций <c>AccommodationRequest.Accommodation</c> догружала бы комнату на каждую строку.
    /// </param>
    internal static ClaimListItemForExportViewModel BuildItemForExport(
        Claim claim, ICurrentUserAccessor currentUserId, ClaimInfo claimInfo, string? roomName)
    {
        var accessArguments = AccessArgumentsFactory.Create(claimInfo, currentUserId.UserIdentificationOrDefault);
        (DateTime lastModifiedAt, var lastModifiedBy) = GetLastComment(claim, accessArguments);
        var balance = claimInfo.ClaimInCharacter.CalculateBalance();
        var projectInfo = claimInfo.ProjectInfo;
        var characterClaim = claimInfo.Claim;
        var playerInfo = claimInfo.Player;

        string? PassportData, RegistrationAddress;
        if (characterClaim.PlayerAllowedSensitiveData && projectInfo.ProfileRequirementSettings.SensitiveDataRequired)
        {
            PassportData = playerInfo.PassportData;
            RegistrationAddress = playerInfo.RegistrationAddress;
        }
        else
        {
            PassportData = RegistrationAddress = null;
        }

        return new ClaimListItemForExportViewModel(
            claimInfo.Character.CharacterName,
            new UserLinkViewModel(playerInfo.ToUserInfoHeader()),
            projectInfo.ProjectName,
            ClaimStatusBuilders.CreateFullStatus(characterClaim, accessArguments),
            lastModifiedAt,
            characterClaim.CreateDate,
            characterClaim.CheckInDate,
            new UserLinkViewModel(projectInfo.GetMasterById(characterClaim.ResponsibleMasterId).UserInfo),
            FeePaid: balance.FeePaid,
            FeeDue: balance.FeeDue,
            TotalFee: balance.TotalFee,
            new UserLinkViewModel(lastModifiedBy),
            claimInfo.ClaimId,
            characterClaim.AccommodationTypeId is { } accommodationTypeId
                ? projectInfo.AccommodationSettings.GetTypeById(accommodationTypeId).Name
                : null,
            roomName,
            characterClaim.Finance.PreferentialFeeUser,
            PassportData,
            RegistrationAddress,
            claimInfo.ClaimInCharacter.GetAllFields().ToDictionary(x => x.Field.Id, x => x.DisplayString),
            playerInfo
            );
    }

    /// <remarks>
    /// <c>User.FullName</c> у EF-сущности для пользователя без ФИО отдаёт пустую строку, а
    /// доменный <see cref="UserFullName.FullName"/> — <c>null</c>. Колонка в списке не nullable,
    /// поэтому сохраняем прежнее поведение.
    /// </remarks>
    private static string GetPlayerFullName(ClaimInfo claimInfo) => claimInfo.Player.UserFullName.FullName ?? "";

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
