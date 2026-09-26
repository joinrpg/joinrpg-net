using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.DomainTypes.ProjectMetadata;

/// <summary>
/// Дерево групп персонажей проекта: сами группы, их топология (родители/дети) и всё, что
/// из этой топологии выводится.
/// </summary>
/// <remarks>
/// Тип неизменяемый: дерево целиком собирается при загрузке метаданных проекта
/// (см. <c>CharacterGroupDictionaryBuilder</c>) и дальше только читается.
/// </remarks>
public sealed class ProjectGroupTree
{
    public ProjectGroupTree(
        CharacterGroupIdentification rootGroupId,
        IReadOnlyDictionary<CharacterGroupIdentification, CharacterGroupInfo> groups)
    {
        GroupsDictionary = groups;
        RootGroupId = rootGroupId;
        AllGroups = [.. groups.Values];

        ResponsibleMasterRules = [.. groups.Values
            .Where(g => g.ResponsibleMasterId != null)
            .OrderByDescending(g => g, Comparer<CharacterGroupInfo>.Create((x, y) =>
            {
                // Более вложенная группа — более специфичное правило, она должна идти раньше.
                if (x.AllParentGroups.Contains(y.Id))
                {
                    return 1;
                }

                if (y.AllParentGroups.Contains(x.Id))
                {
                    return -1;
                }

                return x.Id.CharacterGroupId.CompareTo(y.Id.CharacterGroupId);
            }))];

        AllowToSetGroups = groups.Values.Any(g => g.IsActive && !g.IsRoot && !g.IsSpecial);
    }

    /// <summary>Корневая группа проекта.</summary>
    public CharacterGroupIdentification RootGroupId { get; }

    /// <summary>Все группы проекта, включая корневую, спецгруппы и неактивные.</summary>
    public IReadOnlyCollection<CharacterGroupInfo> AllGroups { get; }

    /// <summary>
    /// Группы с назначенным ответственным мастером, от самых вложенных к самым верхним:
    /// первое подошедшее правило и определяет ответственного мастера.
    /// </summary>
    public IReadOnlyList<CharacterGroupInfo> ResponsibleMasterRules { get; }

    /// <summary>Есть ли в проекте хоть одна живая обычная группа, в которую можно положить персонажа.</summary>
    public bool AllowToSetGroups { get; }

    private IReadOnlyDictionary<CharacterGroupIdentification, CharacterGroupInfo> GroupsDictionary { get; }

    public bool Contains(CharacterGroupIdentification id) => GroupsDictionary.ContainsKey(id);

    /// <summary>
    /// Проверяет изменение группы: строит дерево, каким оно станет, и требует, чтобы изменение
    /// не добавляло новых нарушений правил.
    /// </summary>
    /// <remarks>
    /// Сравнение с нарушениями ДО изменения, а не просто «после изменения нарушений нет»: в старых
    /// данных нарушения есть (issue #4878 — 244 группы в 18 проектах), и чинить чужие ветки мастер
    /// при редактировании этой группы не обязан. Заодно это правильно ловит потомков: снимая
    /// публичность, мастер рвёт путь и публичной ветке под собой — такие нарушения новые.
    /// </remarks>
    /// <param name="isPublic">Публичность ПОСЛЕ изменения.</param>
    /// <param name="directParentIds">Прямые родители ПОСЛЕ изменения.</param>
    /// <exception cref="PublicGroupWithoutPublicPathException" />
    public void ValidateGroupChange(
        CharacterGroupIdentification groupId,
        bool isPublic,
        IReadOnlyCollection<CharacterGroupIdentification> directParentIds)
    {
        var before = PublicGroupPathRule.FindViolations(this).Select(group => group.Id).ToHashSet();

        var newViolations = PublicGroupPathRule
            .FindViolations(WithGroupChange(groupId, isPublic, directParentIds))
            .Where(group => !before.Contains(group.Id))
            .ToList();

        if (newViolations.Count > 0)
        {
            throw new PublicGroupWithoutPublicPathException(
                RootGroupId.ProjectId, [.. newViolations.Select(group => group.Name)]);
        }
    }

    /// <summary>
    /// Проверяет создание группы: публичной новой группе нужен хотя бы один родитель, до которого
    /// есть публичный путь от корня.
    /// </summary>
    /// <remarks>
    /// Копия дерева здесь, в отличие от <see cref="ValidateGroupChange"/>, не нужна: у новой группы
    /// нет потомков, поэтому единственное, что может нарушиться, — её собственный путь наверх.
    /// </remarks>
    /// <param name="name">Название создаваемой группы — только для текста ошибки.</param>
    /// <exception cref="PublicGroupWithoutPublicPathException" />
    public void ValidateGroupCreation(
        string name,
        bool isPublic,
        IReadOnlyCollection<CharacterGroupIdentification> directParentIds)
    {
        if (!isPublic || PublicGroupPathRule.AnyHasPublicPath(this, directParentIds))
        {
            return;
        }

        throw new PublicGroupWithoutPublicPathException(RootGroupId.ProjectId, [name]);
    }

    /// <summary>
    /// Дерево, каким оно станет после изменения группы: топология (прямые дети, все предки, все
    /// потомки) пересчитывается заново.
    /// </summary>
    /// <remarks>
    /// Копия нужна для проверок ДО сохранения, а не для показа. Поэтому порядок дочерних групп
    /// сохраняется как получится — прежний порядок, новые дети в конце, — а тип группы
    /// (обычная/спец) берётся прежний: спецгруппы этим путём не редактируются.
    /// </remarks>
    public ProjectGroupTree WithGroupChange(
        CharacterGroupIdentification groupId,
        bool isPublic,
        IReadOnlyCollection<CharacterGroupIdentification> directParentIds)
    {
        ArgumentNullException.ThrowIfNull(directParentIds);

        if (!GroupsDictionary.ContainsKey(groupId))
        {
            throw new ArgumentException($"Группы {groupId} нет в проекте", nameof(groupId));
        }

        // Родителей берём только существующих: список приходит из формы.
        var changedParents = directParentIds.Where(GroupsDictionary.ContainsKey).ToArray();

        IReadOnlyCollection<CharacterGroupIdentification> ParentsOf(CharacterGroupIdentification id)
            => id == groupId ? changedParents : GroupsDictionary[id].DirectParentGroupIds;

        var childrenOf = AllGroups.ToDictionary(
            group => group.Id,
            group => (IReadOnlyCollection<CharacterGroupIdentification>)
                [
                    // Прежний порядок дочерних групп, затем появившиеся.
                    .. group.DirectChildGroupIds.Where(child => ParentsOf(child).Contains(group.Id)),
                    .. AllGroups
                        .Where(other => ParentsOf(other.Id).Contains(group.Id))
                        .Select(other => other.Id)
                        .Where(other => !group.DirectChildGroupIds.Contains(other)),
                ]);

        var groups = AllGroups.ToDictionary(
            group => group.Id,
            group => group with
            {
                IsPublic = group.Id == groupId ? isPublic : group.IsPublic,
                DirectParentGroupIds = ParentsOf(group.Id),
                DirectChildGroupIds = childrenOf[group.Id],
                AllChildGroups = Closure(group.Id, id => childrenOf[id]),
                AllParentGroups = Closure(group.Id, ParentsOf),
            });

        return new ProjectGroupTree(RootGroupId, groups);
    }

    /// <summary>
    /// Транзитивное замыкание в порядке обхода в глубину, устойчивое к циклам в графе групп.
    /// </summary>
    private static List<CharacterGroupIdentification> Closure(
        CharacterGroupIdentification start,
        Func<CharacterGroupIdentification, IReadOnlyCollection<CharacterGroupIdentification>> next)
    {
        var result = new List<CharacterGroupIdentification>();
        var seen = new HashSet<CharacterGroupIdentification>();
        var stack = new Stack<CharacterGroupIdentification>(next(start).Reverse());

        while (stack.Count > 0)
        {
            var id = stack.Pop();
            if (!seen.Add(id))
            {
                continue;
            }

            result.Add(id);
            foreach (var nextId in next(id).Reverse())
            {
                stack.Push(nextId);
            }
        }

        return result;
    }

    public CharacterGroupInfo GetGroupById(CharacterGroupIdentification id) => GroupsDictionary[id];

    /// <summary>Группа проекта или <c>null</c>, если такой группы нет.</summary>
    public CharacterGroupInfo? GetGroupByIdOrDefault(CharacterGroupIdentification id) => GroupsDictionary.GetValueOrDefault(id);

    public IEnumerable<CharacterGroupInfo> GetGroupsById(IReadOnlyCollection<CharacterGroupIdentification> ids)
    {
        return GroupsDictionary.Where(g => ids.Contains(g.Key)).Select(g => g.Value);
    }

    public IReadOnlyList<CharacterGroupIdentification> GetChildGroupIdsIncludingThis(CharacterGroupIdentification groupId)
    {
        return GroupsDictionary[groupId].AllChildGroupsIncludingThis;
    }

    public IReadOnlyList<CharacterGroupIdentification> GetChildGroupIdsIncludingThis(IEnumerable<CharacterGroupIdentification> groupIds)
    {
        return [.. groupIds.SelectMany(x => GetChildGroupIdsIncludingThis(x)).Distinct()];
    }

    public IEnumerable<CharacterGroupIdentification> GetParentGroupIdsIncludingThis(CharacterGroupIdentification groupId)
    {
        if (!GroupsDictionary.TryGetValue(groupId, out var groupInfo))
        {
            return [];
        }

        return [groupId, .. groupInfo.AllParentGroups];
    }

    public IEnumerable<CharacterGroupIdentification> GetParentGroupIdsIncludingThis(IEnumerable<CharacterGroupIdentification> groupIds)
    {
        return [.. groupIds.SelectMany(x => GetParentGroupIdsIncludingThis(x)).Distinct()];
    }

    public IEnumerable<CharacterGroupInfo> GetParentGroupsIncludingThis(IEnumerable<CharacterGroupIdentification> groupIds)
    {
        var ids = GetParentGroupIdsIncludingThis(groupIds);
        return ids.Select(id => GroupsDictionary[id]);
    }

    public IEnumerable<CharacterGroupInfo> GetDirectChildGroups(CharacterGroupIdentification groupId)
    {
        return GroupsDictionary[groupId].DirectChildGroupIds.Select(id => GroupsDictionary[id]);
    }

    /// <summary>
    /// Группы поддерева <paramref name="groupId"/> (включая саму группу) в порядке упорядоченного DFS.
    /// Порядок уже зашит в <see cref="CharacterGroupInfo.AllChildGroupsIncludingThis"/>: дочерние группы
    /// идут в порядке <c>ChildGroupsOrdering</c>, каждая группа — по первому вхождению. Персонажей здесь нет:
    /// чтобы получить детерминированный порядок персонажей, пройдите по этим группам и отсортируйте прямых
    /// персонажей каждой группы по её <see cref="CharacterGroupInfo.ChildCharactersOrdering"/>.
    /// </summary>
    public IReadOnlyList<CharacterGroupInfo> GetChildGroupsIncludingThis(CharacterGroupIdentification groupId)
    {
        if (!GroupsDictionary.TryGetValue(groupId, out var group))
        {
            return [];
        }
        return [.. group.AllChildGroupsIncludingThis.Select(id => GroupsDictionary[id])];
    }

    /// <summary>
    /// Список групп для персонажа: проверенный, либо — если проект запрещает мастерам выбирать
    /// группы (<see cref="AllowToSetGroups"/>) — корневая группа.
    /// </summary>
    /// <remarks>
    /// Когда <see cref="AllowToSetGroups"/> равно <c>false</c>, переданный список игнорируется
    /// целиком и даже не проверяется. Когда <c>true</c> — список обязателен (пустой считается
    /// ошибкой валидации) и не должен содержать спецгрупп.
    /// </remarks>
    public IReadOnlyCollection<CharacterGroupIdentification> ValidateGroupListForCharacter(
        IReadOnlyCollection<CharacterGroupIdentification> groupIds)
    {
        if (!AllowToSetGroups)
        {
            return [RootGroupId];
        }

        if (groupIds.Count == 0)
        {
            // Обязательный список пуст — это именно «данные невалидны». Раньше здесь работал
            // ServiceValidation.Required из сервисного слоя, сообщение сохранено дословно.
            throw new JoinValidationException($"Required collection of {nameof(CharacterGroupIdentification)} is empty");
        }

        return ValidateCharacterGroupList(groupIds, ensureNotSpecial: true);
    }

    /// <summary>
    /// Проверяет, что все группы принадлежат этому проекту и существуют в дереве (а при
    /// <paramref name="ensureNotSpecial"/> — что среди них нет спецгрупп).
    /// </summary>
    public IReadOnlyCollection<CharacterGroupIdentification> ValidateCharacterGroupList(
        IReadOnlyCollection<CharacterGroupIdentification> groupIds,
        bool ensureNotSpecial = false)
    {
        foreach (var g in groupIds)
        {
            if (g.ProjectId != RootGroupId.ProjectId)
            {
                throw new ArgumentException("Нельзя смешивать разные проекты в запросе!", nameof(groupIds));
            }
        }

        var missing = groupIds
            .Where(id => !Contains(id))
            .ToArray();

        if (missing.Length != 0)
        {
            var missingIds = string.Join(", ", missing.Select(m => m.CharacterGroupId));
            throw new Exception($"Groups {missingIds} doesn't belong to project");
        }

        if (ensureNotSpecial && groupIds.FirstOrDefault(id => GetGroupById(id).IsSpecial) is { } specialGroupId)
        {
            throw new SpecialCharacterGroupNotAllowedException(specialGroupId);
        }

        return groupIds;
    }
}
