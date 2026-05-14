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
        IReadOnlyDictionary<CharacterGroupIdentification, CharacterGroupInfo> groups)
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
    }

    public ProjectFieldInfo GetFieldById(ProjectFieldIdentification id)
    {
        if (id.ProjectId != ProjectId)
        {
            throw new InvalidOperationException();
        }
        return UnsortedFields.Single(f => f.Id.ProjectFieldId == id.ProjectFieldId);
    }

    public bool HasMasterAccess(UserIdentification? userId, Permission permission = Permission.None)
    {
        return Masters.Any(acl => acl.UserId == userId && acl.Permissions.Contains(permission));
    }

    public Permission[] GetMasterAccess(UserIdentification currentUser) => Masters.FirstOrDefault(m => m.UserId == currentUser)?.Permissions ?? [];

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
            ProjectRolesLists, Groups);
    }

    internal ProjectInfo WithChangedStatus(ProjectLifecycleStatus projectLifecycleStatus)
    {
        return new ProjectInfo(ProjectId, ProjectName, FieldsOrdering, UnsortedFields,
            ProjectFieldSettings, ProjectFinanceSettings, AccomodationEnabled,
            AllowToSetGroups,
            RootCharacterGroupId, Masters, PublishPlot, ProjectCheckInSettings,
            projectLifecycleStatus,
            ProjectScheduleSettings, CloneSettings, CreateDate, ProfileRequirementSettings, ClaimSettings,
            ProjectRolesLists, Groups);
    }

    internal ProjectInfo WithAllowManyClaims(bool strictlyOneCharacter)
    {
        return new ProjectInfo(ProjectId, ProjectName, FieldsOrdering, UnsortedFields,
            ProjectFieldSettings, ProjectFinanceSettings, AccomodationEnabled,
            AllowToSetGroups,
            RootCharacterGroupId, Masters, PublishPlot, ProjectCheckInSettings,
            ProjectStatus,
            ProjectScheduleSettings, CloneSettings, CreateDate, ProfileRequirementSettings, ClaimSettings with { StrictlyOneCharacter = strictlyOneCharacter },
            ProjectRolesLists, Groups);
    }

    public IEnumerable<CharacterGroupIdentification> GetChildGroupIdsIncludingThis(CharacterGroupIdentification groupId)
    {
        if (!Groups.TryGetValue(groupId, out var groupInfo))
        {
            return [];
        }

        return [groupId, .. groupInfo.AllChildGroups];
    }

    public IEnumerable<CharacterGroupIdentification> GetChildGroupIdsIncludingThis(IEnumerable<CharacterGroupIdentification> groupIds)
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
