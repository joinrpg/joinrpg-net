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
}

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
