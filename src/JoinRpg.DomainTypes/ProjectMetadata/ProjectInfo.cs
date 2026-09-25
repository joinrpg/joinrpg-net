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

    /// <summary>Дерево групп персонажей проекта.</summary>
    public ProjectGroupTree GroupTree { get; }

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

    public ProjectRolesListIdentification? DefaultRolesListId { get; }

    public ProjectInfo(
        ProjectIdentification projectId,
        ProjectName projectName,
        string ordering,
        IReadOnlyCollection<ProjectFieldInfo> unsortedFields,
        ProjectFieldSettings projectFieldSettings,
        ProjectFinanceSettings projectFinanceSettings,
        bool accomodationEnabled,
        ProjectGroupTree groupTree,
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
        ProjectRolesListIdentification? defaultRolesListId)
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

        GroupTree = groupTree;
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
        DefaultRolesListId = defaultRolesListId;
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

    public bool HasEditRolesAccess(UserIdentification? userId)
    {
        return HasMasterAccess(userId, Permission.CanEditRoles) && IsActive;
    }

    // For tests
    internal ProjectInfo WithAddedField(ProjectFieldInfo field)
    {
        ProjectFieldInfo[] fields = [field, .. UnsortedFields];

        return new ProjectInfo(ProjectId, ProjectName, FieldsOrdering, fields,
            ProjectFieldSettings, ProjectFinanceSettings, AccomodationEnabled, GroupTree,
            Masters, PublishPlot, ProjectCheckInSettings, ProjectStatus,
            ProjectScheduleSettings, CloneSettings, CreateDate, ProfileRequirementSettings, ClaimSettings,
            ProjectRolesLists, DefaultRolesListId);
    }

    internal ProjectInfo WithChangedStatus(ProjectLifecycleStatus projectLifecycleStatus)
    {
        return new ProjectInfo(ProjectId, ProjectName, FieldsOrdering, UnsortedFields,
            ProjectFieldSettings, ProjectFinanceSettings, AccomodationEnabled,
            GroupTree,
            Masters, PublishPlot, ProjectCheckInSettings,
            projectLifecycleStatus,
            ProjectScheduleSettings, CloneSettings, CreateDate, ProfileRequirementSettings, ClaimSettings,
            ProjectRolesLists, DefaultRolesListId);
    }

    internal ProjectInfo WithAllowManyClaims(bool strictlyOneCharacter)
    {
        return new ProjectInfo(ProjectId, ProjectName, FieldsOrdering, UnsortedFields,
            ProjectFieldSettings, ProjectFinanceSettings, AccomodationEnabled,
            GroupTree,
            Masters, PublishPlot, ProjectCheckInSettings,
            ProjectStatus,
            ProjectScheduleSettings, CloneSettings, CreateDate, ProfileRequirementSettings, ClaimSettings with { StrictlyOneCharacter = strictlyOneCharacter },
            ProjectRolesLists, DefaultRolesListId);
    }

    internal ProjectInfo WithProfileRequirementSettings(ProjectProfileRequirementSettings profileRequirementSettings)
    {
        return new ProjectInfo(ProjectId, ProjectName, FieldsOrdering, UnsortedFields,
            ProjectFieldSettings, ProjectFinanceSettings, AccomodationEnabled,
            GroupTree,
            Masters, PublishPlot, ProjectCheckInSettings,
            ProjectStatus,
            ProjectScheduleSettings, CloneSettings, CreateDate, profileRequirementSettings, ClaimSettings,
            ProjectRolesLists, DefaultRolesListId);
    }

    /// <summary>
    /// Группа проекта по «сырому» идентификатору. Единственный метод про группы, который остаётся
    /// на <see cref="ProjectInfo"/>: только здесь известен <see cref="ProjectId"/>, чтобы собрать
    /// <see cref="CharacterGroupIdentification"/>.
    /// </summary>
    public CharacterGroupInfo GetGroupById(int id)
        => GroupTree.GetGroupById(new CharacterGroupIdentification(ProjectId, id));

    public ProjectRolesList GetRolesListById(ProjectRolesListIdentification id)
    {
        return ProjectRolesLists.SingleOrDefault(x => x.ProjectRolesListId == id)
            ?? throw new KeyNotFoundException("Не найдена сетка ролей с ID=" + id);
    }

    public ProjectInfo EnsureProjectActive() => !IsActive ? throw new ProjectDeactivatedException(ProjectId) : this;

    public ProjectMasterInfo SelectResponsibleMaster(IEnumerable<CharacterGroupIdentification> allCharacterGroups)
    {
        foreach (var rule in GroupTree.ResponsibleMasterRules)
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
