namespace JoinRpg.Data.Interfaces;

public interface ICharacterGroupRepository
{
    Task<CharacterGroupFullInfo?> GetCharacterGroupFullInfo(CharacterGroupIdentification id);

    Task<IReadOnlyList<CharacterGroupFullInfo>> GetCharacterGroupsFullInfo(
        IReadOnlyCollection<CharacterGroupIdentification> groupIds);

    /// <summary>
    /// Проекты, где есть публичная группа с непубличным прямым родителем.
    /// </summary>
    /// <remarks>
    /// Это кандидаты на нарушение правила публичного пути (issue #4878), а не сами нарушения:
    /// нарушение считается по всему дереву (<c>PublicGroupPathRule</c>), но если оно есть, то
    /// где-то на пути наверх обязательно найдётся именно такое прямое ребро — значит проект
    /// с нарушением сюда попадёт. Проверять дерево целиком дешевле уже по этому короткому списку,
    /// а не по всем 1700 проектам.
    /// </remarks>
    [Obsolete("Разовая починка данных под #4878, удалить вместе с ней — см. #4974")]
    Task<IReadOnlyCollection<ProjectIdentification>> GetProjectsWithPublicGroupUnderPrivateParent();
}
