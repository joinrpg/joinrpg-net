namespace JoinRpg.XGameApi.Contract;

/// <summary>
/// Полная картина проекта для LLM (ADR012): поля, варианты значений и дерево групп персонажей,
/// включая спецгруппы полей/вариантов — по ним строится <c>list_characters</c> по группе
/// («кто относится к партии синих» — это группа варианта, а не фильтрация всех персонажей).
/// </summary>
public class ProjectOverview
{
    public int ProjectId { get; set; }

    public required string ProjectName { get; set; }

    public required IEnumerable<ProjectFieldInfo> Fields { get; set; }

    public required IEnumerable<ProjectOverviewGroup> Groups { get; set; }
}

/// <summary>
/// Группа персонажей в дереве проекта, включая спецгруппы (см. <see cref="ProjectOverview"/>).
/// </summary>
public class ProjectOverviewGroup
{
    public int CharacterGroupId { get; set; }

    public required string CharacterGroupName { get; set; }

    /// <summary>Непосредственные родительские группы (могут быть множественными).</summary>
    public required IReadOnlyCollection<int> DirectParentGroupIds { get; set; }

    /// <summary>
    /// Спецгруппа поля (значения которого приводят персонажа в неё) или варианта значения —
    /// в неё нельзя добавить персонажа явно, только через значение поля.
    /// </summary>
    public bool IsSpecial { get; set; }
}
