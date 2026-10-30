using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.DataModel;
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
       IClaimProblemValidator claimValidator, ClaimInfo claimInfo, Dictionary<CommentDiscussionId, int> unreadComments)
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
            unreadComments.GetValueOrDefault(claimInfo.Claim.CommentDiscussionId),
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
        var sensitiveData = claimInfo.SensitiveData;

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
            sensitiveData.PassportData,
            sensitiveData.RegistrationAddress,
            sensitiveData.BirthDate,
            claimInfo.ClaimInCharacter.GetAllFields().ToDictionary(x => x.Field.Id, x => x.ExportString),
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
        var (at, by) = GetLastCommentCore(
            claim.CreateDate, claim.LastPlayerCommentAt, claim.LastVisibleMasterCommentAt,
            claim.LastMasterCommentAt, accessArguments);

        var user = by switch
        {
            LastCommentBy.Player => claim.Player,
            LastCommentBy.VisibleMaster => claim.LastVisibleMasterCommentBy!,
            _ => claim.LastMasterCommentBy!,
        };
        return (at.UtcDateTime, user.ToUserInfoHeader());
    }

    /// <summary>
    /// То же, что для EF-сущности, но поверх доменного агрегата (ADR013).
    /// </summary>
    public static DateTime GetLastCommentTime(CharacterClaimInfo claim, AccessArguments accessArguments)
        => GetLastCommentCore(
            claim.CreateDate, claim.LastPlayerCommentAt, claim.LastVisibleMasterCommentAt,
            claim.LastMasterCommentAt, accessArguments).At.UtcDateTime;

    public static DateTime GetLastCommentTime(Claim claim, AccessArguments accessArguments)
        => GetLastCommentCore(
            claim.CreateDate, claim.LastPlayerCommentAt, claim.LastVisibleMasterCommentAt,
            claim.LastMasterCommentAt, accessArguments).At.UtcDateTime;

    private enum LastCommentBy
    {
        Player,
        VisibleMaster,
        AnyMaster,
    }

    /// <summary>
    /// Время последней активности по заявке: подача, последний комментарий игрока, последний
    /// видимый игроку комментарий мастера, а мастеру — ещё и любой мастерский.
    /// </summary>
    /// <remarks>
    /// <see cref="Claim.CreateDate"/> лежит в колонке <c>datetime</c> и приезжает с
    /// <see cref="DateTimeKind.Unspecified"/>, а отметки комментариев — <see cref="DateTimeOffset"/>.
    /// Считаем дату создания UTC (она так и записывается) и сравниваем всё в одном типе: неявное
    /// приведение <see cref="DateTime"/> к <see cref="DateTimeOffset"/> приклеило бы к дате
    /// локальное смещение сервера и сдвинуло выбор победителя.
    /// </remarks>
    private static (DateTimeOffset At, LastCommentBy By) GetLastCommentCore(
        DateTime createDate,
        DateTimeOffset? lastPlayerCommentAt,
        DateTimeOffset? lastVisibleMasterCommentAt,
        DateTimeOffset? lastMasterCommentAt,
        AccessArguments accessArguments)
    {
        var lastComment = (At: new DateTimeOffset(DateTime.SpecifyKind(createDate, DateTimeKind.Utc)), By: LastCommentBy.Player);

        if (lastPlayerCommentAt is not null && lastPlayerCommentAt > lastComment.At)
        {
            lastComment = (At: lastPlayerCommentAt.Value, By: LastCommentBy.Player);
        }

        if (lastVisibleMasterCommentAt is not null && lastVisibleMasterCommentAt > lastComment.At)
        {
            lastComment = (At: lastVisibleMasterCommentAt.Value, By: LastCommentBy.VisibleMaster);
        }

        if (accessArguments.MasterAccess && lastMasterCommentAt is not null && lastMasterCommentAt > lastComment.At)
        {
            lastComment = (At: lastMasterCommentAt.Value, By: LastCommentBy.AnyMaster);
        }

        return lastComment;
    }
}
