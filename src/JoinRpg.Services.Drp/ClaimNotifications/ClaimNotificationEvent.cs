using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.Services.Drp.ClaimNotifications;

/// <summary>
/// Что за уведомление по заявке.
/// </summary>
public enum ClaimNotificationKind
{
    /// <summary>
    /// Комментарий или действие по заявке: «Заявка {персонаж} игрока {игрок} {действие} {кем}». Сюда же
    /// попадают ручные финансовые операции — оплата и возврат взноса мастером (<see cref="CommentExtraAction.PaidFee"/>,
    /// <see cref="CommentExtraAction.RefundFee"/> с <see cref="ClaimNotificationEvent.Money"/>), переводы,
    /// установка взноса и модерация оплат: они пишутся как обычный комментарий с действием.
    /// </summary>
    ClaimChange,

    /// <summary>
    /// Онлайн-оплата (и подписки, возвраты через банк): ссылка на финансовую операцию, в теле только
    /// текст платёжного сервиса, без приветствия и без строки «Заявка …».
    /// </summary>
    OnlinePayment,

    /// <summary>
    /// Приглашение к совместному проживанию: «{Проект}: приглашения к проживанию». Ссылается на заявку,
    /// но заявку не меняет — восстанавливать по нему нечего.
    /// </summary>
    AccommodationInvite,

    /// <summary>
    /// Ссылается на заявку, но не разобралось.
    /// </summary>
    Other,
}

/// <summary>
/// Кто совершил действие по заявке — по концу второй строки тела («мастером X» или «игроком»).
/// </summary>
public enum ClaimEventAuthor
{
    Player,
    Master,
}

/// <param name="UserId">Получатель уведомления.</param>
/// <param name="GreetingName">Имя из приветствия «Добрый день, …» — отображаемое имя получателя на момент события.</param>
public record ClaimNotificationRecipient(UserIdentification UserId, string? GreetingName);

/// <summary>
/// Одно событие по заявке. В базе уведомлений на каждого получателя своя строка — здесь они уже склеены.
/// </summary>
/// <param name="Reference">Исходная ссылка уведомления: заявка, комментарий к ней или финансовая операция.</param>
/// <param name="Initiator">Кто вызвал событие. Сам инициатор среди получателей не бывает.</param>
/// <param name="CharacterName">Имя персонажа на момент события (заголовок строится после сохранения).</param>
/// <param name="PlayerName">Отображаемое имя игрока на момент события.</param>
/// <param name="Action">Действие; null — простой комментарий или не <see cref="ClaimNotificationKind.ClaimChange"/>.</param>
/// <param name="MasterName">Имя мастера из «мастером X», если действовал мастер.</param>
/// <param name="Money">«Сумма операции: N» у денежных действий.</param>
/// <param name="Text">Текст комментария (без служебных строк).</param>
public record ClaimNotificationEvent(
    ClaimIdentification ClaimId,
    IProjectEntityId Reference,
    DateTimeOffset CreatedAt,
    UserIdentification Initiator,
    ClaimNotificationKind Kind,
    string? CharacterName,
    string? PlayerName,
    CommentExtraAction? Action,
    ClaimEventAuthor? Author,
    string? MasterName,
    int? Money,
    string Text,
    IReadOnlyList<ClaimNotificationRecipient> Recipients)
{
    /// <summary>
    /// Id комментария, если ссылка на комментарий (с 2026-06-06); до этого все события ссылались на заявку.
    /// </summary>
    public int? CommentId => (Reference as DomainTypes.Claims.ClaimCommentIdentification)?.CommentId;
}
