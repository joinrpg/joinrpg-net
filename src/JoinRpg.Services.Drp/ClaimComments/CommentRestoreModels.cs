using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Drp.ClaimNotifications;

namespace JoinRpg.Services.Drp.ClaimComments;

/// <summary>Комментарий обсуждения заявки в восстановленной базе.</summary>
/// <param name="IsVisibleToPlayer">
/// Виден ли игроку — нужно, чтобы понять, отвечал ли игрок на видимый комментарий мастера
/// (<see cref="CommentToRestore.VisibilityQuestionable"/>).
/// </param>
/// <param name="Text">
/// Текст — для сопоставления с событиями без id. null — не загружен (комментарий задолго до бэкапа,
/// с событиями после бэкапа он не совпадёт по времени) или его нет: такой комментарий ни с чем не совпадает.
/// </param>
public record ExistingClaimCommentInfo(
    int CommentId,
    ClaimIdentification ClaimId,
    UserIdentification AuthorUserId,
    DateTimeOffset CreatedAt,
    bool IsCommentByPlayer,
    bool IsVisibleToPlayer,
    string? Text);

/// <summary>Всё, по чему строится план восстановления комментариев к заявкам одного проекта (#5492).</summary>
/// <param name="Events">События по заявкам проекта (<see cref="ClaimNotificationParser"/>).</param>
/// <param name="Claims">Заявки проекта, которые есть в базе: id заявки → её игрок.</param>
/// <param name="Masters">
/// Мастера проекта. Мастер, играющий в своём проекте, получает уведомления и о скрытых комментариях
/// (как подписчик или ответственный) — видимость в его заявках не определить.
/// </param>
/// <param name="ExistingComments">Комментарии обсуждений этих заявок, которые есть в базе.</param>
/// <param name="TakenCommentIds">
/// Id комментариев из уведомлений, которые в базе заняты не комментариями заявок из <paramref name="Claims"/>
/// (чужое обсуждение, форум, другой проект): с таким id комментарий вставить нельзя.
/// </param>
/// <param name="BackupTakenAt">Момент бэкапа: события до него в восстановленной базе уже есть.</param>
public record CommentRestoreSource(
    IReadOnlyCollection<ClaimNotificationEvent> Events,
    IReadOnlyDictionary<ClaimIdentification, UserIdentification> Claims,
    IReadOnlyCollection<UserIdentification> Masters,
    IReadOnlyCollection<ExistingClaimCommentInfo> ExistingComments,
    IReadOnlyCollection<int> TakenCommentIds,
    DateTimeOffset BackupTakenAt);

/// <summary>Комментарий, которого нет в базе, — вставить.</summary>
/// <param name="CommentId">Прежний id; null — в уведомлении его не было (до 2026-06-06), база выдаст новый.</param>
/// <param name="ExtraAction">Действие, которое комментарий сопровождал; текста у такого комментария может не быть.</param>
/// <param name="IsVisibleToPlayer">Виден ли игроку: комментарий игрока — всегда, мастерский — если игрок получил уведомление.</param>
/// <param name="VisibilityQuestionable">
/// Мастерский комментарий посчитан видимым, но это мог быть и скрытый — проверить вручную. Так бывает,
/// если после предыдущего видимого комментария мастера писал игрок (до фикса #5497 игрок получал
/// уведомление и о скрытом ответе на свой комментарий) или если игрок заявки — мастер проекта.
/// </param>
public record CommentToRestore(
    int? CommentId,
    UserIdentification Author,
    bool IsCommentByPlayer,
    CommentExtraAction? ExtraAction,
    DateTimeOffset CreatedAt,
    string Text,
    bool IsVisibleToPlayer,
    bool VisibilityQuestionable);

/// <summary>Что мешает восстановить комментарии заявки.</summary>
public enum CommentRestoreWarning
{
    /// <summary>Заявки нет в базе — сначала восстановить заявки, потом комментарии.</summary>
    ClaimNotFound,

    /// <summary>
    /// Id комментария из уведомления в базе занят чем-то другим (<see cref="CommentRestorePlanRow.Conflicts"/>):
    /// такой комментарий не вставляется.
    /// </summary>
    CommentIdConflict,

    /// <summary>
    /// Есть уведомления по заявке, которые не разобрались (<see cref="CommentRestorePlanRow.Unparsed"/>):
    /// возможно, потерян комментарий — посмотреть вручную.
    /// </summary>
    UnparsedEvents,
}

/// <summary>План восстановления комментариев одной заявки.</summary>
/// <param name="ClaimExists">Заявка есть в базе; если нет — <see cref="ToRestore"/> пуст.</param>
/// <param name="ToRestore">Комментарии для вставки, по времени.</param>
/// <param name="SkippedExisting">Сколько комментариев из уведомлений уже есть в базе.</param>
/// <param name="Conflicts">Id комментариев, занятые в базе другим (<see cref="CommentRestoreWarning.CommentIdConflict"/>).</param>
/// <param name="SkippedFinance">
/// Сколько финансовых событий (в том числе онлайн-оплат) пропущено — их восстанавливают отдельно (фаза 9).
/// </param>
/// <param name="Unparsed">Сколько уведомлений по заявке не разобралось (<see cref="CommentRestoreWarning.UnparsedEvents"/>).</param>
public record CommentRestorePlanRow(
    ClaimIdentification ClaimId,
    bool ClaimExists,
    IReadOnlyList<CommentToRestore> ToRestore,
    int SkippedExisting,
    IReadOnlyCollection<int> Conflicts,
    int SkippedFinance,
    int Unparsed,
    IReadOnlyCollection<CommentRestoreWarning> Warnings);
