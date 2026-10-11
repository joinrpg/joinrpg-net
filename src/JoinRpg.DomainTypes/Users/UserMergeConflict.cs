using JoinRpg.Helpers;

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

    /// <summary>Поглощаемый — сам админ, который сливает: удалять собственный аккаунт этим инструментом нельзя.</summary>
    public sealed record SecondaryIsCurrentUser : UserMergeConflict;

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

    /// <summary>
    /// В базе есть заблокированный проект (ADR023) — идёт восстановление данных. Пока оно не
    /// закончено, сливать нельзя никого: восстановление может вставить строки со ссылкой на уже
    /// удалённого поглощаемого.
    /// </summary>
    public sealed record BlockedProject(ProjectIdentification ProjectId) : UserMergeConflict;

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

/// <summary>
/// Слияние запрошено, хотя предпросмотр нашёл конфликты. Интерфейс до такого не доводит — это
/// гонка (данные поменялись после предпросмотра) или обход интерфейса.
/// </summary>
public class UserMergeConflictException(UserMergePreview preview)
    : JoinRpgBaseException($"Нельзя слить {preview.SecondaryUserId} в {preview.PrimaryUserId}: конфликтов {preview.Conflicts.Count}")
{
    public UserMergePreview Preview { get; } = preview;
}
