using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Services.Interfaces.Drp;

/// <summary>
/// Каким заявка была на момент потери основной БД — по базе уведомлений (DRP, #5481). null — не менять.
/// </summary>
/// <param name="Character">Персонаж, на котором заявка оказалась (перемещение после бэкапа).</param>
/// <param name="Status">Статус после последнего статусного события.</param>
/// <param name="StatusChangedAt">Когда статус стал таким — им проставляется отметка даты статуса.</param>
/// <param name="ResponsibleMaster">Ответственный мастер; обязан быть мастером проекта.</param>
/// <param name="ManualFee">Взнос, установленный вручную.</param>
public record RestoredClaimTarget(
    CharacterIdentification? Character,
    ClaimStatus? Status,
    DateTimeOffset? StatusChangedAt,
    UserIdentification? ResponsibleMaster,
    int? ManualFee);

/// <summary>
/// Восстановление заявок по базе уведомлений после потери основной БД (DRP-план, фаза 7, #5481).
/// Только в заблокированном проекте (ADR023), без уведомлений, с записью в истории заявки.
/// </summary>
public interface IClaimRestoreService
{
    /// <summary>
    /// Приводит заявку, которая есть в восстановленной базе, к состоянию на момент аварии. Повторный
    /// вызов с тем же состоянием ничего не меняет и в историю не пишет.
    /// </summary>
    Task ApplyRestoredState(ClaimIdentification claimId, RestoredClaimTarget target);

    /// <summary>
    /// Создаёт заявку, которой нет в восстановленной базе, <b>с прежним идентификатором</b> — на него
    /// ссылаются уведомления, письма и выгрузки — и приводит её к состоянию на момент аварии.
    /// </summary>
    /// <returns>
    /// Персонаж заявки — найденный или созданный: следующие потерянные заявки на того же нового
    /// персонажа надо подавать уже на него, а не создавать второго.
    /// </returns>
    Task<CharacterIdentification> RestoreLostClaim(LostClaimToRestore claim);

    /// <summary>
    /// Возвращает в обсуждение заявки комментарии, которые есть только в уведомлениях (DRP, #5492), —
    /// с прежними идентификаторами, авторами и датами, без уведомлений. Отметки заявки о последних
    /// комментариях подтягиваются к восстановленным, дата обновления заявки (а с ней и проекта)
    /// становится текущей, в историю заявки пишется одна запись. Повторный вызов с теми же
    /// комментариями ничего не вставляет и в историю не пишет.
    /// </summary>
    Task<RestoredCommentsResult> RestoreClaimComments(ClaimIdentification claimId, IReadOnlyList<RestoredClaimComment> comments);
}

/// <summary>
/// Комментарий к заявке, восстановленный по уведомлению.
/// </summary>
/// <param name="CommentId">
/// Прежний идентификатор либо <c>null</c>: до 2026-06-06 id комментария в уведомлениях не было — такой
/// комментарий получит новый id, а повтор узнаётся по автору, времени и тексту.
/// </param>
/// <param name="Author">Автор; обязан быть существующим пользователем.</param>
/// <param name="CreatedAt">Когда комментарий был написан; не из будущего.</param>
public record RestoredClaimComment(
    int? CommentId,
    UserIdentification Author,
    bool IsCommentByPlayer,
    bool IsVisibleToPlayer,
    CommentExtraAction? ExtraAction,
    DateTimeOffset CreatedAt,
    string Text);

/// <summary>
/// Итог восстановления комментариев заявки.
/// </summary>
/// <param name="Inserted">Сколько комментариев вставлено.</param>
/// <param name="AlreadyExisted">Сколько уже было в обсуждении этой заявки (повтор).</param>
/// <param name="Conflicts">
/// Прежние id, которые вставить нельзя: заняты комментарием другого обсуждения либо не потерянные
/// (не ниже текущего счётчика). Такие комментарии пропущены.
/// </param>
public record RestoredCommentsResult(int Inserted, int AlreadyExisted, IReadOnlyList<int> Conflicts);

/// <summary>
/// Потерянная заявка: подана после бэкапа, в восстановленной базе её нет.
/// </summary>
/// <param name="ClaimId">Прежний идентификатор из уведомлений.</param>
/// <param name="Character">
/// Персонаж из восстановленной базы, на которого была заявка, либо null — тогда создаётся новый
/// персонаж игрока с именем <paramref name="NewCharacterName"/> в корневой группе.
/// </param>
/// <param name="CreatedAt">Когда заявка была подана — первое событие по ней.</param>
public record LostClaimToRestore(
    ClaimIdentification ClaimId,
    CharacterIdentification? Character,
    string? NewCharacterName,
    UserIdentification Player,
    UserIdentification ResponsibleMaster,
    DateTimeOffset CreatedAt,
    RestoredClaimTarget Target);
