using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.ProjectMetadata.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.DomainTypes.Test;

/// <summary>
/// Сборка <see cref="ProjectInfo"/> и его частей для тестов. Конструктор <see cref="ProjectInfo"/>
/// принимает два десятка аргументов, поэтому собирать его в каждом тестовом классе заново нельзя —
/// копии неизбежно разъедутся.
/// </summary>
internal static class ProjectInfoFixture
{
    public static readonly ProjectIdentification ProjectId = new(1);
    public static readonly CharacterGroupIdentification RootGroupId = new(ProjectId, 1);
    public static readonly UserIdentification DefaultMasterId = new(100);

    public static ProjectInfo MakeProject(params ProjectFieldInfo[] fields)
        => MakeProject("", fields);

    public static ProjectInfo MakeProject(string ordering, params ProjectFieldInfo[] fields)
        => Build(ordering: ordering, fields: fields);

    public static ProjectInfo Build(
        string ordering = "",
        IReadOnlyCollection<ProjectFieldInfo>? fields = null,
        ProjectGroupTree? groupTree = null,
        IReadOnlyCollection<ProjectMasterInfo>? masters = null,
        ProjectLifecycleStatus projectStatus = ProjectLifecycleStatus.ActiveClaimsOpen,
        IReadOnlyCollection<ProjectFeeSettingInfo>? feeSchedule = null,
        ProjectAccommodationSettings? accommodationSettings = null,
        bool scheduleEnabled = false)
        => new(
            ProjectId,
            new ProjectName("Test"),
            ordering,
            fields ?? [],
            new ProjectFieldSettings(null, null),
            new ProjectFinanceSettings(
                PreferentialFeeEnabled: false,
                WarnOnOverPayment: true,
                PaymentTypes: [],
                FeeSchedule: feeSchedule ?? []),
            accommodationSettings ?? new ProjectAccommodationSettings(false, []),
            groupTree ?? new ProjectGroupTree(RootGroupId, new Dictionary<CharacterGroupIdentification, CharacterGroupInfo>()),
            masters ?? [MakeMaster(DefaultMasterId, isOwner: true)],
            false,
            new ProjectCheckInSettings(false, false, false),
            projectStatus,
            new ProjectScheduleSettings(scheduleEnabled),
            ProjectCloneSettings.CloneDisabled,
            new DateOnly(2024, 1, 1),
            ProjectProfileRequirementSettings.AllNotRequired,
            new ProjectClaimSettings(null, false, false, false, false),
            [],
            null,
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow"));

    public static ProjectMasterInfo MakeMaster(UserIdentification userId, bool isOwner = false)
        => new(
            userId,
            new UserDisplayName($"Master{userId.Value}", null),
            new Email($"master{userId.Value}@example.com"),
            [Permission.None],
            isOwner);

    public static ProjectFieldInfo MakeField(
        int fieldId,
        ProjectFieldType type = ProjectFieldType.String,
        ProjectFieldVisibility visibility = ProjectFieldVisibility.Public,
        FieldBoundTo boundTo = FieldBoundTo.Character,
        string ordering = "",
        IReadOnlyCollection<ProjectFieldVariant>? variants = null)
        => new(
            new ProjectFieldIdentification(ProjectId, fieldId),
            $"Field{fieldId}",
            type,
            boundTo,
            variants ?? [],
            ordering,
            0,
            true,
            true,
            MandatoryStatus.Optional,
            true,
            true,
            [],
            null,
            null,
            false,
            new ProjectFieldSettings(null, null),
            null,
            visibility,
            null,
            WasEverUsed: false);

    public static CharacterGroupIdentification GroupId(int id) => new(ProjectId, id);

    /// <summary>
    /// Тип проживания для тестов. По умолчанию категория комнат совпадает с id типа — ровно так,
    /// как их сегодня заполняет маппер метаданных, пока тип и категория не разделены (ADR018).
    /// </summary>
    public static AccommodationTypeInfo MakeAccommodationType(
        int typeId,
        int capacity,
        int? roomCategoryId = null,
        string? name = null,
        int cost = 1000,
        bool isPlayerSelectable = true)
        => new(
            new AccommodationTypeIdentification(ProjectId, typeId),
            new RoomCategoryIdentification(ProjectId, roomCategoryId ?? typeId),
            name ?? $"Тип проживания {typeId}",
            new MarkdownString(""),
            cost,
            capacity,
            isPlayerSelectable);

    /// <summary>
    /// Строит дерево групп по описанию «группа → её прямые родители», досчитывая транзитивные
    /// замыкания <c>AllParentGroups</c> / <c>AllChildGroups</c> и обратные связи. Корневая группа
    /// (<see cref="RootGroupId"/>) добавляется всегда и родителей не имеет.
    /// </summary>
    public static ProjectGroupTree MakeGroupTree(
        IReadOnlyDictionary<int, int[]> directParentsByGroup,
        IReadOnlyDictionary<int, CharacterGroupType>? types = null,
        IReadOnlyDictionary<int, bool>? isActiveByGroup = null,
        IReadOnlyDictionary<int, UserIdentification>? responsibleMasterByGroup = null,
        IReadOnlyDictionary<int, bool>? isPublicByGroup = null)
        => new(RootGroupId, MakeGroups(directParentsByGroup, types, isActiveByGroup, responsibleMasterByGroup, isPublicByGroup));

    private static Dictionary<CharacterGroupIdentification, CharacterGroupInfo> MakeGroups(
        IReadOnlyDictionary<int, int[]> directParentsByGroup,
        IReadOnlyDictionary<int, CharacterGroupType>? types,
        IReadOnlyDictionary<int, bool>? isActiveByGroup,
        IReadOnlyDictionary<int, UserIdentification>? responsibleMasterByGroup,
        IReadOnlyDictionary<int, bool>? isPublicByGroup = null)
    {
        var directParents = new Dictionary<int, int[]>(directParentsByGroup);
        directParents.TryAdd(RootGroupId.CharacterGroupId, []);

        var allIds = directParents.Keys.ToArray();

        var directChildren = allIds.ToDictionary(
            id => id,
            id => allIds.Where(other => directParents[other].Contains(id)).ToArray());

        return allIds.ToDictionary(
            GroupId,
            id =>
            {
                // GetValueOrDefault здесь нельзя: default(CharacterGroupType) == Root.
                var groupType = types is not null && types.TryGetValue(id, out var explicitType)
                    ? explicitType
                    : id == RootGroupId.CharacterGroupId ? CharacterGroupType.Root : CharacterGroupType.Regular;

                return new CharacterGroupInfo(
                    GroupId(id),
                    $"Group{id}",
                    isActiveByGroup?.GetValueOrDefault(id, true) ?? true,
                    IsPublic: isPublicByGroup?.GetValueOrDefault(id, true) ?? true,
                    [.. directChildren[id].Select(GroupId)],
                    ChildCharactersOrdering: "",
                    [.. directParents[id].Select(GroupId)],
                    [.. Closure(id, x => directChildren[x]).Select(GroupId)],
                    [.. Closure(id, x => directParents[x]).Select(GroupId)],
                    groupType,
                    responsibleMasterByGroup?.GetValueOrDefault(id));
            });

        // Транзитивное замыкание без самой вершины; циклы в тестовых данных не зациклят обход.
        IReadOnlyList<int> Closure(int start, Func<int, IReadOnlyList<int>> step)
        {
            var seen = new HashSet<int>();
            var queue = new Queue<int>(step(start));
            var result = new List<int>();
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == start || !seen.Add(current))
                {
                    continue;
                }
                result.Add(current);
                foreach (var next in step(current))
                {
                    queue.Enqueue(next);
                }
            }
            return result;
        }
    }

    public static readonly AccessArguments AccessArgumentsNone = AccessArguments.None;

    public static readonly AccessArguments AccessArgumentsPlayer = new(
        MasterAccess: false,
        PlayerAccessToCharacter: true,
        PlayerAccesToClaim: false,
        EditAllowed: false,
        Published: false,
        CharacterPublic: false,
        IsCapitan: false);

    public static readonly AccessArguments AccessArgumentsMaster = new(
        MasterAccess: true,
        PlayerAccessToCharacter: false,
        PlayerAccesToClaim: false,
        EditAllowed: false,
        Published: false,
        CharacterPublic: false,
        IsCapitan: false);
}
