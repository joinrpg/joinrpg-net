using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;

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
    /// персонажа надо подавать уже на него, а не создавать второго. Для шаблона — сам шаблон, даже
    /// если принятая заявка переехала на своего персонажа из него.
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

    /// <summary>
    /// Возвращает в заявку ручные финансовые операции (взносы и возвраты на типы оплаты Cash/Custom) и
    /// решения мастеров по ним, которые есть только в уведомлениях (DRP, #5561), — с прежними
    /// идентификаторами, авторами и датами, без уведомлений. Решение по операции, которая уже есть в базе
    /// и ждёт его, переводит её в итоговое состояние. Взнос фиксируется, как при обычной оплате,
    /// отметки заявки о последних комментариях подтягиваются к восстановленным, дата обновления заявки
    /// (а с ней и проекта) становится текущей, в историю заявки пишется одна запись. Повторный вызов с
    /// тем же входом ничего не вставляет, не меняет и в историю не пишет.
    /// </summary>
    /// <remarks>
    /// Все проверки входа — до первой записи: неверный вход не оставляет ничего вставленным.
    /// </remarks>
    Task<RestoredFinanceResult> RestoreFinanceOperations(
        ClaimIdentification claimId,
        IReadOnlyList<RestoredFinanceOperation> operations,
        IReadOnlyList<RestoredFinanceModeration> moderations);
}

/// <summary>
/// Ручная финансовая операция заявки, восстановленная по уведомлению.
/// </summary>
/// <param name="OperationId">
/// Прежний идентификатор (он же идентификатор комментария операции) либо <c>null</c>: до 2026-06-06 id в
/// уведомлениях не было — такая операция получит новый id, а повтор узнаётся по автору, виду, сумме и
/// времени (в пределах минуты).
/// </param>
/// <param name="Author">Инициатор: мастер, принявший деньги, или игрок, сообщивший об оплате.</param>
/// <param name="Type">Только <see cref="FinanceOperationType.Submit"/> или <see cref="FinanceOperationType.Refund"/>.</param>
/// <param name="Money">Со знаком, как в базе: взнос — положительная, возврат — отрицательная.</param>
/// <param name="PaymentType">Неонлайн-тип оплаты (Cash/Custom) этого проекта; может быть и выключенным.</param>
/// <param name="State">
/// Итоговое состояние; решение по этой операции обязано с ним совпадать. Если операция уже была в базе в
/// ожидании решения (прошлый прогон без решения), решение переведёт её. Возврат — только
/// <see cref="FinanceOperationState.Approved"/>, как у доменного возврата.
/// </param>
/// <param name="CreatedAt">Время операции; по нему же ставится дата операции (в часовом поясе проекта).</param>
/// <param name="ChangedAt">Время решения по операции, а без решения — <paramref name="CreatedAt"/>.</param>
public record RestoredFinanceOperation(
    int? OperationId,
    UserIdentification Author,
    bool IsCommentByPlayer,
    FinanceOperationType Type,
    int Money,
    PaymentTypeIdentification PaymentType,
    FinanceOperationState State,
    DateTimeOffset CreatedAt,
    DateTimeOffset ChangedAt,
    string Text);

/// <summary>
/// Решение мастера по финансовой операции («подтверждена»/«отклонена»), восстановленное по уведомлению.
/// Вставляется комментарием-ответом на операцию. Родитель задаётся не более чем одним способом; без
/// родителя — решение, которое не удалось связать с операцией (не найдена, неоднозначно, запрос льготы
/// есть только в уведомлениях): оно вставляется комментарием без родителя и ничьё состояние не меняет.
/// </summary>
/// <param name="CommentId">Прежний идентификатор комментария решения либо <c>null</c> — получит новый.</param>
/// <param name="Author">Мастер, принявший решение.</param>
/// <param name="Approved"><c>true</c> — подтверждена, <c>false</c> — отклонена.</param>
/// <param name="ParentOperationId">
/// Операция с известным id: уже есть в заявке либо восстанавливается этим же вызовом. Уже бывшая в базе —
/// только заявленный платёж (Submit) или запрос льготного взноса. Если это платёж, который был в базе до
/// вызова и ждёт решения, решение переводит его в итоговое состояние; у запроса льготного взноса
/// восстанавливается только комментарий. У операции, вставляемой этим вызовом, состояние обязано
/// совпадать с решением.
/// </param>
/// <param name="ParentRestoredIndex">Индекс в списке операций вызова — для восстанавливаемой операции без id.</param>
public record RestoredFinanceModeration(
    int? CommentId,
    UserIdentification Author,
    bool Approved,
    DateTimeOffset CreatedAt,
    string Text,
    int? ParentOperationId,
    int? ParentRestoredIndex);

/// <summary>Исход восстановления одной операции или одного решения.</summary>
public enum RestoredFinanceEntryStatus
{
    /// <summary>Вставлено этим вызовом.</summary>
    Inserted,

    /// <summary>Уже было в заявке (повтор) — ничего не сделано.</summary>
    AlreadyExisted,

    /// <summary>Прежний id занят другим комментарием (другой заявки, форума, без операции) — пропущено.</summary>
    TakenElsewhere,

    /// <summary>Прежний id не ниже текущего счётчика — это не потерянный id; пропущено.</summary>
    NotLostId,

    /// <summary>Решение пропущено: его операция не восстановлена из-за конфликта.</summary>
    ParentNotRestored,
}

/// <summary>Исход восстановления одной операции или одного решения.</summary>
/// <param name="Id">
/// У операции — вставленный (в том числе новый — для операции без прежнего id) или уже бывший, а при
/// конфликте — запрошенный. У решения — прежний из входа (<c>null</c>, если его не было).
/// </param>
public record RestoredFinanceEntry(RestoredFinanceEntryStatus Status, int? Id)
{
    /// <summary>Запись теперь есть в заявке — вставлена или уже была.</summary>
    public bool IsPresent => Status is RestoredFinanceEntryStatus.Inserted or RestoredFinanceEntryStatus.AlreadyExisted;
}

/// <summary>
/// Итог восстановления финансовых операций заявки: исход по каждой записи — в порядке входных списков.
/// </summary>
public record RestoredFinanceResult(IReadOnlyList<RestoredFinanceEntry> Operations, IReadOnlyList<RestoredFinanceEntry> Moderations)
{
    /// <summary>Сколько записей (операций и решений) вставлено.</summary>
    public int Inserted => Operations.Concat(Moderations).Count(e => e.Status == RestoredFinanceEntryStatus.Inserted);

    /// <summary>Сколько записей уже было в заявке.</summary>
    public int AlreadyExisted => Operations.Concat(Moderations).Count(e => e.Status == RestoredFinanceEntryStatus.AlreadyExisted);

    /// <summary>Сколько записей пропущено из-за номера: занят другим или не потерянный.</summary>
    public int Conflicts => Operations.Concat(Moderations)
        .Count(e => e.Status is RestoredFinanceEntryStatus.TakenElsewhere or RestoredFinanceEntryStatus.NotLostId);

    /// <summary>Сколько решений пропущено: их операция не восстановлена.</summary>
    public int ParentsNotRestored => Moderations.Count(e => e.Status == RestoredFinanceEntryStatus.ParentNotRestored);
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
/// персонаж с именем <paramref name="NewCharacterName"/> в корневой группе.
/// </param>
/// <param name="CreatedAt">Когда заявка была подана — первое событие по ней.</param>
/// <param name="NewCharacterVisibility">
/// Видимость нового персонажа (при <paramref name="Character"/> = null): в уведомлениях её нет,
/// выбирает мастер (#5677). Персонаж из шаблона при принятии наследует её от шаблона.
/// </param>
/// <param name="NewCharacterIsSlot">
/// Новый персонаж (при <paramref name="Character"/> = null) создаётся шаблоном, а не персонажем игрока.
/// Принятая заявка на шаблон, как при одобрении, получает своего персонажа из шаблона; заявка в
/// обсуждении остаётся на шаблоне.
/// </param>
public record LostClaimToRestore(
    ClaimIdentification ClaimId,
    CharacterIdentification? Character,
    string? NewCharacterName,
    UserIdentification Player,
    UserIdentification ResponsibleMaster,
    DateTimeOffset CreatedAt,
    RestoredClaimTarget Target,
    CharacterVisibility NewCharacterVisibility,
    bool NewCharacterIsSlot = false);
