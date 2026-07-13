using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Helpers;

namespace JoinRpg.DomainTypes.ProjectMetadata;

public record class ProjectInfo
{
    private readonly Lazy<VirtualOrderContainer<ProjectFieldInfo>> sortedFieldsContainer;

    public IReadOnlyList<ProjectFieldInfo> SortedFields => sortedFieldsContainer.Value.OrderedItems;

    private readonly Lazy<VirtualOrderContainer<ProjectFieldInfo>> sortedActiveFieldsContainer;

    public IReadOnlyList<ProjectFieldInfo> SortedActiveFields => sortedActiveFieldsContainer.Value.OrderedItems;

    public ProjectIdentification ProjectId { get; }
    public ProjectName ProjectName { get; }
    public IReadOnlyCollection<ProjectFieldInfo> UnsortedFields { get; }
    public ProjectFieldInfo? CharacterNameField { get; }
    public ProjectFieldInfo? CharacterDescriptionField { get; }

    public ProjectFieldInfo? TimeSlotField { get; }
    public ProjectFieldInfo? RoomField { get; }

    public ProjectFieldSettings ProjectFieldSettings { get; }
    public ProjectFinanceSettings ProjectFinanceSettings { get; }
    public bool AccomodationEnabled { get; }

    public CharacterIdentification? DefaultTemplateCharacter { get; }
    public bool AllowToSetGroups { get; }

    public CharacterGroupIdentification RootCharacterGroupId { get; }
    public IReadOnlyCollection<ProjectMasterInfo> Masters { get; }
    public string FieldsOrdering { get; }

    public bool PublishPlot { get; }
    public ProjectCloneSettings CloneSettings { get; }
    public ProjectCheckInSettings ProjectCheckInSettings { get; }

    public ProjectLifecycleStatus ProjectStatus { get; }
    public bool IsActive => ProjectStatus != ProjectLifecycleStatus.Archived;

    public DateOnly CreateDate { get; }

    public ProjectScheduleSettings ProjectScheduleSettings { get; }

    public ProjectProfileRequirementSettings ProfileRequirementSettings { get; }
    public ProjectClaimSettings ClaimSettings { get; }
    public IReadOnlyCollection<ProjectRolesList> ProjectRolesLists { get; }

    public IReadOnlyDictionary<CharacterGroupIdentification, CharacterGroupInfo> Groups { get; }

    public IReadOnlyList<CharacterGroupInfo> ResponsibleMasterRules { get; }

    public ProjectInfo(
        ProjectIdentification projectId,
        ProjectName projectName,
        string ordering,
        IReadOnlyCollection<ProjectFieldInfo> unsortedFields,
        ProjectFieldSettings projectFieldSettings,
        ProjectFinanceSettings projectFinanceSettings,
        bool accomodationEnabled,
        bool allowToSetGroups,
        CharacterGroupIdentification rootCharacterGroupId,
        IReadOnlyCollection<ProjectMasterInfo> masters,
        bool publishPlot,
        ProjectCheckInSettings projectCheckInSettings,
        ProjectLifecycleStatus projectStatus,
        ProjectScheduleSettings projectScheduleSettings,
        ProjectCloneSettings projectCloneSettings,
        DateOnly createDate,
        ProjectProfileRequirementSettings profileRequirementSettings,
        ProjectClaimSettings projectClaimSettings,
        IReadOnlyCollection<ProjectRolesList> projectRolesLists,
        IReadOnlyDictionary<CharacterGroupIdentification, CharacterGroupInfo> groups,
        IReadOnlyList<CharacterGroupInfo> responsibleMasterRules)
    {
        UnsortedFields = unsortedFields;
        ProjectId = projectId;
        ProjectName = projectName;
        FieldsOrdering = ordering;
        sortedFieldsContainer = VirtualOrderContainerFacade.CreateLazy(unsortedFields, ordering);
        sortedActiveFieldsContainer = VirtualOrderContainerFacade.CreateLazy(unsortedFields.Where(f => f.IsActive), ordering);
        ProjectFieldSettings = projectFieldSettings;
        ProjectFinanceSettings = projectFinanceSettings;
        AccomodationEnabled = accomodationEnabled;
        CharacterNameField = projectFieldSettings.NameField is ProjectFieldIdentification nameField ? GetFieldById(nameField) : null;

        CharacterDescriptionField = projectFieldSettings.DescriptionField is ProjectFieldIdentification descriptionField ? GetFieldById(descriptionField) : null;

        TimeSlotField = UnsortedFields.SingleOrDefault(f => f.Type == ProjectFieldType.ScheduleTimeSlotField && f.IsActive);
        RoomField = UnsortedFields.SingleOrDefault(f => f.Type == ProjectFieldType.ScheduleRoomField && f.IsActive);

        AllowToSetGroups = allowToSetGroups;
        RootCharacterGroupId = rootCharacterGroupId;
        Masters = masters;
        PublishPlot = publishPlot;
        ProjectCheckInSettings = projectCheckInSettings;
        ProjectStatus = projectStatus;
        ProjectScheduleSettings = projectScheduleSettings;
        CloneSettings = projectCloneSettings;
        CreateDate = createDate;
        ProfileRequirementSettings = profileRequirementSettings;
        ClaimSettings = projectClaimSettings;
        ProjectRolesLists = projectRolesLists;
        Groups = groups;
        ResponsibleMasterRules = responsibleMasterRules;
    }

    public ProjectFieldInfo GetFieldById(ProjectFieldIdentification id)
    {
        if (id.ProjectId != ProjectId)
        {
            throw new InvalidOperationException();
        }
        return UnsortedFields.SingleOrDefault(f => f.Id.ProjectFieldId == id.ProjectFieldId) ?? throw new KeyNotFoundException("Не найдено поле с ID=" + id);
    }

    public ProjectFieldVariant? GetVariantByGroupIdOrDefault(CharacterGroupIdentification characterGroupIdentification)
    {
        return UnsortedFields.SelectMany(pf => pf.Variants).SingleOrDefault(pfv => pfv.CharacterGroupId == characterGroupIdentification);
    }

    public bool HasMasterAccess(UserIdentification? userId, Permission permission = Permission.None)
    {
        return Masters.Any(acl => acl.UserId == userId && acl.Permissions.Contains(permission));
    }

    public Permission[] GetMasterAccess(UserIdentification currentUser) => Masters.FirstOrDefault(m => m.UserId == currentUser)?.Permissions ?? [];

    public ProjectMasterInfo GetMasterById(UserIdentification currentUser) => Masters.First(m => m.UserId == currentUser);

    public ProjectMasterInfo GetDefaultResponsibleMaster()
        => Masters.FirstOrDefault(m => m.IsOwner)
            ?? Masters.OrderBy(m => m.UserId.Value).First();

    public bool HasEditRolesAccess(UserIdentification userId)
    {
        return HasMasterAccess(userId, Permission.CanEditRoles) && IsActive;
    }

    // For tests
    internal ProjectInfo WithAddedField(ProjectFieldInfo field)
    {
        ProjectFieldInfo[] fields = [field, .. UnsortedFields];

        return new ProjectInfo(ProjectId, ProjectName, FieldsOrdering, fields,
            ProjectFieldSettings, ProjectFinanceSettings, AccomodationEnabled, AllowToSetGroups,
            RootCharacterGroupId, Masters, PublishPlot, ProjectCheckInSettings, ProjectStatus,
            ProjectScheduleSettings, CloneSettings, CreateDate, ProfileRequirementSettings, ClaimSettings,
            ProjectRolesLists, Groups, ResponsibleMasterRules);
    }

    internal ProjectInfo WithChangedStatus(ProjectLifecycleStatus projectLifecycleStatus)
    {
        return new ProjectInfo(ProjectId, ProjectName, FieldsOrdering, UnsortedFields,
            ProjectFieldSettings, ProjectFinanceSettings, AccomodationEnabled,
            AllowToSetGroups,
            RootCharacterGroupId, Masters, PublishPlot, ProjectCheckInSettings,
            projectLifecycleStatus,
            ProjectScheduleSettings, CloneSettings, CreateDate, ProfileRequirementSettings, ClaimSettings,
            ProjectRolesLists, Groups, ResponsibleMasterRules);
    }

    internal ProjectInfo WithAllowManyClaims(bool strictlyOneCharacter)
    {
        return new ProjectInfo(ProjectId, ProjectName, FieldsOrdering, UnsortedFields,
            ProjectFieldSettings, ProjectFinanceSettings, AccomodationEnabled,
            AllowToSetGroups,
            RootCharacterGroupId, Masters, PublishPlot, ProjectCheckInSettings,
            ProjectStatus,
            ProjectScheduleSettings, CloneSettings, CreateDate, ProfileRequirementSettings, ClaimSettings with { StrictlyOneCharacter = strictlyOneCharacter },
            ProjectRolesLists, Groups, ResponsibleMasterRules);
    }

    public CharacterGroupInfo GetGroupById(int id)
    {
        var groupId = new CharacterGroupIdentification(ProjectId, id);
        return Groups[groupId];
    }

    public CharacterGroupInfo GetGroupById(CharacterGroupIdentification id) => Groups[id];

    public ProjectRolesList GetRolesListById(ProjectRolesListIdentification id)
    {
        return ProjectRolesLists.SingleOrDefault(x => x.ProjectRolesListId == id)
            ?? throw new KeyNotFoundException("Не найдена сетка ролей с ID=" + id);
    }

    public IEnumerable<CharacterGroupInfo> GetGroupsById(IReadOnlyCollection<CharacterGroupIdentification> ids)
    {
        return Groups.Where(g => ids.Contains(g.Key)).Select(g => g.Value);
    }

    public IReadOnlyList<CharacterGroupIdentification> GetChildGroupIdsIncludingThis(CharacterGroupIdentification groupId)
    {
        return Groups[groupId].AllChildGroupsIncludingThis;
    }

    public IReadOnlyList<CharacterGroupIdentification> GetChildGroupIdsIncludingThis(IEnumerable<CharacterGroupIdentification> groupIds)
    {
        return [.. groupIds.SelectMany(x => GetChildGroupIdsIncludingThis(x)).Distinct()];
    }

    public IEnumerable<CharacterGroupIdentification> GetParentGroupIdsIncludingThis(CharacterGroupIdentification groupId)
    {
        if (!Groups.TryGetValue(groupId, out var groupInfo))
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
        return ids.Select(id => Groups[id]);
    }

    public IEnumerable<CharacterGroupInfo> GetDirectChildGroups(CharacterGroupIdentification groupId)
    {
        return Groups[groupId].DirectChildGroupIds.Select(id => Groups[id]);
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
        if (!Groups.TryGetValue(groupId, out var group))
        {
            return [];
        }
        return [.. group.AllChildGroupsIncludingThis.Select(id => Groups[id])];
    }

    public ProjectInfo EnsureProjectActive() => !IsActive ? throw new ProjectDeactivatedException(ProjectId) : this;

    public ProjectMasterInfo SelectResponsibleMaster(IEnumerable<CharacterGroupIdentification> allCharacterGroups)
    {
        foreach (var rule in ResponsibleMasterRules)
        {
            if (allCharacterGroups.Contains(rule.Id))
            {
                return GetMasterById(rule.ResponsibleMasterId!);
            }
        }

        return GetDefaultResponsibleMaster();
    }
}

public record ProjectProfileRequirementSettings(
    MandatoryStatus RequireRealName,
    MandatoryStatus RequireTelegram,
    MandatoryStatus RequireVkontakte,
    MandatoryStatus RequirePhone,
    MandatoryStatus RequirePassport,
    MandatoryStatus RequireRegistrationAddress)
{
    public static readonly ProjectProfileRequirementSettings AllNotRequired
        = new(MandatoryStatus.Optional, MandatoryStatus.Optional, MandatoryStatus.Optional, MandatoryStatus.Optional, MandatoryStatus.Optional, MandatoryStatus.Optional);

    public bool SensitiveDataRequired => RequirePassport != MandatoryStatus.Optional || RequireRegistrationAddress != MandatoryStatus.Optional;
}

public record ProjectClaimSettings(
    CharacterIdentification? DefaultTemplate,
    bool StrictlyOneCharacter,
    bool AutoAcceptClaims,
    bool IsAcceptingClaims,
    bool IsPublicProject);
