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
}
