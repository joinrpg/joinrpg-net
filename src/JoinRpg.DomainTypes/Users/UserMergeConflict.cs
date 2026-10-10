namespace JoinRpg.DomainTypes.Users;

/// <summary>
/// Причина, по которой два аккаунта нельзя слить механически (ADR024). Инструмент слияния ничего
/// не решает за людей; часть конфликтов они могут снять сами, часть — нет (см. ADR024).
/// </summary>
public abstract record UserMergeConflict
{
    private UserMergeConflict() { }

    /// <summary>Основной и поглощаемый аккаунт — один и тот же пользователь.</summary>
    public sealed record SameUser : UserMergeConflict;

    /// <summary>Служебный пользователь (робот, платежи) — его сливать нельзя.</summary>
    public sealed record VirtualUser(UserIdentification UserId) : UserMergeConflict;

    /// <summary>Оба аккаунта есть среди мастеров (в том числе бывших) одного проекта.</summary>
    public sealed record BothMasters(ProjectIdentification ProjectId) : UserMergeConflict;

    /// <summary>У обоих активные заявки на одного персонажа.</summary>
    public sealed record BothClaimedCharacter(CharacterIdentification CharacterId) : UserMergeConflict;

    /// <summary>
    /// В проекте, где игроку разрешён только один персонаж, у одного аккаунта утверждённая заявка,
    /// а у другого — тоже активная (утверждённая или нет).
    /// </summary>
    public sealed record ApprovedClaimInSingleCharacterProject(ProjectIdentification ProjectId) : UserMergeConflict;

    /// <summary>У обоих касса (тип оплаты «наличные») в одном проекте.</summary>
    public sealed record BothHaveCashPaymentType(ProjectIdentification ProjectId) : UserMergeConflict;

    /// <summary>Аккаунты переводили деньги друг другу — после слияния отправитель совпал бы с получателем.</summary>
    public sealed record MoneyTransfersBetween(ProjectIdentification ProjectId) : UserMergeConflict;

    /// <summary>Оба аккаунта — капитаны одной группы.</summary>
    public sealed record BothCaptains(CharacterGroupIdentification CharacterGroupId) : UserMergeConflict;
}

/// <summary>
/// Что произойдёт при слиянии <paramref name="SecondaryUserId"/> в <paramref name="PrimaryUserId"/> (ADR024).
/// </summary>
/// <param name="Conflicts">Слияние возможно, только если список пуст.</param>
/// <param name="DroppedLoginProviders">
/// Провайдеры внешнего входа, привязанные к обоим аккаунтам. Остаётся привязка основного, а вход
/// через соцсеть поглощаемого перестанет работать.
/// </param>
public record UserMergePreview(
    UserIdentification PrimaryUserId,
    UserIdentification SecondaryUserId,
    IReadOnlyCollection<UserMergeConflict> Conflicts,
    IReadOnlyCollection<string> DroppedLoginProviders,
    UserMergeSecondaryStats SecondaryStats)
{
    public bool CanMerge => Conflicts.Count == 0;
}

/// <summary>
/// Сколько всего переедет с поглощаемого аккаунта — чтобы админ видел масштаб до подтверждения.
/// </summary>
public record UserMergeSecondaryStats(int Claims, int Comments, int MasterProjects);
