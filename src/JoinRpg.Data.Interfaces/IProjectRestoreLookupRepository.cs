using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Data.Interfaces;

/// <summary>
/// Лёгкие выборки для плана восстановления проекта по выгрузкам мастера (DRP, #5529).
/// Временный код: удаляется вместе с <c>JoinRpg.Services.Drp</c>.
/// </summary>
public interface IProjectRestoreLookupRepository
{
    /// <summary>
    /// Какие из групп восстанавливаемого проекта (прежние id из выгрузки) создать с этим id нельзя:
    /// номер занят группой другого проекта.
    /// </summary>
    /// <param name="groupIds">Id групп одного проекта.</param>
    /// <returns>Подмножество <paramref name="groupIds"/>.</returns>
    Task<IReadOnlyCollection<CharacterGroupIdentification>> GetGroupsTakenByOtherProjects(IReadOnlyCollection<CharacterGroupIdentification> groupIds);

    /// <summary>
    /// Какие из персонажей восстанавливаемого проекта (прежние id из выгрузки) создать с этим id нельзя:
    /// номер занят персонажем другого проекта.
    /// </summary>
    /// <param name="characterIds">Id персонажей одного проекта.</param>
    /// <returns>Подмножество <paramref name="characterIds"/>.</returns>
    Task<IReadOnlyCollection<CharacterIdentification>> GetCharactersTakenByOtherProjects(IReadOnlyCollection<CharacterIdentification> characterIds);

    /// <summary>Пользователи по email пачкой, без учёта регистра. Кого не нашли — того в словаре нет.</summary>
    Task<IReadOnlyDictionary<string, UserIdentification>> FindUsersByEmails(IReadOnlyCollection<string> emails);

    /// <summary>
    /// Всё из базы, что нужно плану восстановления комментариев к заявкам проекта (DRP, #5492): заявки
    /// (любого статуса), комментарии их обсуждений и какие id комментариев из уведомлений заняты чем-то другим.
    /// </summary>
    /// <param name="notifiedCommentIds">Id комментариев, которые встретились в уведомлениях по заявкам проекта.</param>
    /// <param name="textsSince">
    /// Тексты комментариев грузятся только для созданных не раньше этого момента (у остальных — null):
    /// они нужны лишь для сопоставления с событиями после бэкапа.
    /// </param>
    Task<ClaimCommentsRestoreLookup> GetClaimCommentsForRestore(ProjectIdentification projectId, IReadOnlyCollection<int> notifiedCommentIds, DateTimeOffset textsSince);

    /// <summary>
    /// То же по одной заявке: её комментарии, а занятыми считаются id из уведомлений, которых нет в её обсуждении.
    /// Заявки нет в проекте — <see cref="ClaimCommentsRestoreLookup.Claims"/> пуст.
    /// </summary>
    Task<ClaimCommentsRestoreLookup> GetClaimCommentsForRestore(ClaimIdentification claimId, IReadOnlyCollection<int> notifiedCommentIds, DateTimeOffset textsSince);

    /// <summary>
    /// Всё из базы, что нужно плану восстановления ручных финансовых операций проекта (DRP, #5561):
    /// заявки (любого статуса), их финансовые операции и финансовые комментарии, какие id из уведомлений
    /// заняты чем-то другим, типы оплаты проекта и мастера с правом на деньги.
    /// </summary>
    /// <param name="notifiedCommentIds">Id комментариев (операций), которые встретились в уведомлениях по заявкам проекта.</param>
    Task<ClaimFinanceRestoreLookup> GetClaimFinanceForRestore(ProjectIdentification projectId, IReadOnlyCollection<int> notifiedCommentIds);

    /// <summary>Заблокированные проекты (ADR023, <see cref="ProjectLifecycleStatus.Blocked"/>) в порядке id.</summary>
    Task<IReadOnlyList<ProjectIdentification>> GetBlockedProjects();
}

/// <summary>Финансовая операция заявки в базе — для плана восстановления ручных операций.</summary>
/// <param name="OperationId">Id операции — он же id её комментария.</param>
/// <param name="CreatedAt">Когда операция создана (<c>FinanceOperation.Created</c>), в UTC.</param>
public record ClaimFinanceOperationForRestore(
    int OperationId,
    ClaimIdentification ClaimId,
    FinanceOperationType Type,
    FinanceOperationState State,
    int Money,
    DateTimeOffset CreatedAt);

/// <summary>
/// Финансовый комментарий заявки в базе: с действием <see cref="CommentExtraAction.PaidFee"/>,
/// <see cref="CommentExtraAction.RefundFee"/>, <see cref="CommentExtraAction.ApproveFinance"/> или
/// <see cref="CommentExtraAction.RejectFinance"/>.
/// </summary>
/// <param name="CreatedAt">Время создания в UTC.</param>
public record ClaimFinanceCommentForRestore(
    int CommentId,
    ClaimIdentification ClaimId,
    UserIdentification Author,
    DateTimeOffset CreatedAt,
    CommentExtraAction ExtraAction);

/// <summary>Тип оплаты проекта (в том числе выключенный).</summary>
public record PaymentTypeForRestore(
    PaymentTypeIdentification PaymentTypeId,
    PaymentTypeKind Kind,
    UserIdentification Owner,
    bool IsActive,
    string Name);

/// <summary>Действующий мастер проекта с правом на деньги.</summary>
public record MoneyMasterForRestore(UserIdentification UserId, bool IsOwner);

/// <param name="Claims">Все заявки проекта.</param>
/// <param name="Operations">Все финансовые операции этих заявок.</param>
/// <param name="Comments">Финансовые комментарии этих заявок (см. <see cref="ClaimFinanceCommentForRestore"/>).</param>
/// <param name="TakenCommentIds">
/// Id из уведомлений, под которыми в базе есть комментарий, но не из <paramref name="Comments"/> и не операция
/// из <paramref name="Operations"/>: нефинансовый комментарий той же заявки, чужое обсуждение, форум, другой проект.
/// </param>
/// <param name="PaymentTypes">Все типы оплаты проекта.</param>
/// <param name="MoneyMasters">Действующие мастера проекта с правом на деньги.</param>
public record ClaimFinanceRestoreLookup(
    IReadOnlyList<ClaimIdentification> Claims,
    IReadOnlyList<ClaimFinanceOperationForRestore> Operations,
    IReadOnlyList<ClaimFinanceCommentForRestore> Comments,
    IReadOnlyCollection<int> TakenCommentIds,
    IReadOnlyList<PaymentTypeForRestore> PaymentTypes,
    IReadOnlyList<MoneyMasterForRestore> MoneyMasters);

/// <summary>Заявка проекта в базе — для плана восстановления комментариев.</summary>
public record ClaimForCommentRestore(ClaimIdentification ClaimId, UserIdentification Player, string CharacterName);

/// <summary>Комментарий обсуждения заявки в базе.</summary>
/// <param name="CreatedAt">Время создания в UTC.</param>
/// <param name="Text">Текст; null — комментарий старше запрошенного момента или строки текста в базе нет.</param>
public record ClaimCommentForRestore(
    int CommentId,
    ClaimIdentification ClaimId,
    UserIdentification Author,
    DateTimeOffset CreatedAt,
    bool IsCommentByPlayer,
    bool IsVisibleToPlayer,
    string? Text);

/// <param name="Claims">Заявки (все заявки проекта либо одна запрошенная).</param>
/// <param name="Comments">Комментарии обсуждений этих заявок.</param>
/// <param name="TakenCommentIds">
/// Id из уведомлений, под которыми в базе есть комментарий, но не из обсуждений <paramref name="Claims"/>
/// (чужое обсуждение, форум, другой проект).
/// </param>
public record ClaimCommentsRestoreLookup(
    IReadOnlyList<ClaimForCommentRestore> Claims,
    IReadOnlyList<ClaimCommentForRestore> Comments,
    IReadOnlyCollection<int> TakenCommentIds);
