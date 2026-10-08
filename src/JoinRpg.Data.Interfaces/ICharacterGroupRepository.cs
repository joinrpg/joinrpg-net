namespace JoinRpg.Data.Interfaces;

public interface ICharacterGroupRepository
{
    Task<CharacterGroupFullInfo?> GetCharacterGroupFullInfo(CharacterGroupIdentification id);

    Task<IReadOnlyList<CharacterGroupFullInfo>> GetCharacterGroupsFullInfo(
        IReadOnlyCollection<CharacterGroupIdentification> groupIds);

    /// <summary>
    /// Проекты, где есть публичная группа без единого живого публичного родителя.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Это кандидаты на нарушение правила публичного пути (issue #4878), а не сами нарушения:
    /// нарушение считается по всему дереву (<c>PublicGroupPathRule</c>), а проверять дерево
    /// целиком дешевле по короткому списку, а не по всем 1700 проектам.
    /// </para>
    /// <para>
    /// Условие необходимое: если публичная группа недостижима от корня, пойдём от неё вверх по
    /// живым публичным родителям. Наверх мы так не выйдем (иначе группа была бы достижима),
    /// значит цепочка закончится группой, у которой живого публичного родителя нет, — и она
    /// в том же проекте. Так ловятся оба случая: и непубличный родитель, и оборванная ветка,
    /// где родитель удалён.
    /// </para>
    /// <para>
    /// Единственная дырка — цикл из публичных групп, замкнутый сам на себя: у каждой группы
    /// родитель есть, а до корня не дойти. На проде таких нет (обход вверх с MAXRECURSION 50
    /// не упирался в предел), и сервисы такую конфигурацию создать не дают.
    /// </para>
    /// </remarks>
    [Obsolete("Разовая починка данных под #4878, удалить вместе с ней — см. #4974")]
    Task<IReadOnlyCollection<ProjectIdentification>> GetProjectsWithPublicGroupWithoutPublicParent();
}
