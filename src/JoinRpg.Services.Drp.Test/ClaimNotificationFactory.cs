using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Services.Impl.Claims;

namespace JoinRpg.Services.Drp.Test;

/// <summary>
/// Строит строки базы уведомлений так же, как прод: тексты — настоящим <see cref="ClaimNotificationTextBuilder"/>,
/// по строке на получателя с подставленным приветствием (<c>NotificationServiceImpl</c>). Если формат
/// уведомлений поменяется, тесты разбора упадут, а не разойдутся с ним молча.
/// </summary>
internal static class ClaimNotificationFactory
{
    public static readonly ProjectIdentification Project = new(7);
    public static readonly ClaimIdentification Claim = new(Project, 1234);
    public const string ProjectTitle = "Игра: продолжение";

    public static readonly UserInfoHeader Player = User(10, "Игрок Игроков");
    public static readonly UserInfoHeader Master = User(20, "Мастер Мастеров");
    public static readonly UserInfoHeader OtherMaster = User(21, "Другой Мастер");

    public static UserInfoHeader User(int id, string name) => new(new UserIdentification(id), new UserDisplayName(name, null), Avatar: null);

    private static readonly ClaimNotificationTextBuilder TextBuilder = new();

    private static int nextCommentId = 100;

    /// <summary>Уведомление о комментарии или действии по заявке, по строке на каждого получателя.</summary>
    public static IEnumerable<NotificationMessageForRecipient> ClaimChange(
        DateTimeOffset at,
        CommentExtraAction? action,
        UserInfoHeader initiator,
        IEnumerable<UserInfoHeader> recipients,
        string text = "текст",
        string characterName = "Персонаж",
        UserInfoHeader? player = null,
        int? money = null,
        ClaimIdentification? claimId = null,
        IProjectEntityId? reference = null)
    {
        var claim = new ClaimWithPlayer
        {
            ClaimId = claimId ?? Claim,
            CharacterName = characterName,
            Player = player ?? Player,
            ExtraNicknames = null,
            ResponsibleMasterUserId = Master.UserId,
            CharacterId = new CharacterIdentification(Project, 1),
        };
        var operationType = initiator == (player ?? Player) ? ClaimOperationType.PlayerChange : ClaimOperationType.MasterVisibleChange;
        var model = new ClaimSimpleChangedNotification(claim.ClaimId, action, initiator, new NotificationEventTemplate(text), operationType, Money: money);

        var body = TextBuilder.GetText(model, claim);
        var header = TextBuilder.GetClaimEmailTitle(new DomainTypes.ProjectMetadata.ProjectName(ProjectTitle), claim);
        var entityReference = reference ?? new ClaimCommentIdentification(claim.ClaimId, nextCommentId++);

        return recipients.Select(r => Row(header, body.Replace("%recepient.name%", r.DisplayName.DisplayName), initiator, r, entityReference, at));
    }

    /// <summary>Приглашение к совместному проживанию (формат <c>AccommodationNotificationTextBuilder</c>): ссылается на заявку получателя.</summary>
    public static NotificationMessageForRecipient AccommodationInvite(DateTimeOffset at)
        => Row(
            $"{ProjectTitle}: приглашения к проживанию",
            $"Добрый день, {Player.DisplayName.DisplayName}\n\n{OtherMaster.DisplayName.DisplayName} отправил Вам приглашение к совместному проживанию.\n\nУправлять приглашениями можно на странице Вашей заявки.",
            OtherMaster, Player, Claim, at);

    public static NotificationMessageForRecipient Row(string header, string body, UserInfoHeader initiator, UserInfoHeader recipient, IProjectEntityId? reference, DateTimeOffset at)
        => new(new MarkdownString(body), initiator.UserId, header, recipient.UserId, reference, at);

    public static DateTimeOffset Day(int day) => new(2026, 8, day, 12, 0, 0, TimeSpan.Zero);
}
