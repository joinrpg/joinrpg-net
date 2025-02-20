using JoinRpg.Helpers;

namespace JoinRpg.PrimitiveTypes.ProjectMetadata;
public record class ProjectInfo
{
    private readonly Lazy<VirtualOrderContainer<ProjectFieldInfo>> sortedFieldsContainer;

    public IReadOnlyList<ProjectFieldInfo> SortedFields => sortedFieldsContainer.Value.OrderedItems;

    private readonly Lazy<VirtualOrderContainer<ProjectFieldInfo>> sortedActiveFieldsContainer;

    public IReadOnlyList<ProjectFieldInfo> SortedActiveFields => sortedActiveFieldsContainer.Value.OrderedItems;

    public ProjectIdentification ProjectId { get; }
    public string ProjectName { get; }
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
    public ProjectCheckInSettings ProjectCheckInSettings { get; }

    public ProjectLifecycleStatus ProjectStatus { get; }
    public ProjectScheduleSettings ProjectScheduleSettings { get; }

    public ProjectInfo(
        ProjectIdentification projectId,
        string projectName,
        string ordering,
        IReadOnlyCollection<ProjectFieldInfo> unsortedFields,
        ProjectFieldSettings projectFieldSettings,
        ProjectFinanceSettings projectFinanceSettings,
        bool accomodationEnabled,
        CharacterIdentification? defaultTemplateCharacter,
        bool allowToSetGroups,
        CharacterGroupIdentification rootCharacterGroupId,
        IReadOnlyCollection<ProjectMasterInfo> masters,
        bool publishPlot,
        ProjectCheckInSettings projectCheckInSettings,
        ProjectLifecycleStatus projectStatus,
        ProjectScheduleSettings projectScheduleSettings)
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

        DefaultTemplateCharacter = defaultTemplateCharacter;
        AllowToSetGroups = allowToSetGroups;
        RootCharacterGroupId = rootCharacterGroupId;
        Masters = masters;
        PublishPlot = publishPlot;
        ProjectCheckInSettings = projectCheckInSettings;
        ProjectStatus = projectStatus;
        ProjectScheduleSettings = projectScheduleSettings;
    }

    public ProjectFieldInfo GetFieldById(ProjectFieldIdentification id)
    {
        if (id.ProjectId != ProjectId)
        {
            throw new InvalidOperationException();
        }
        return UnsortedFields.Single(f => f.Id.ProjectFieldId == id.ProjectFieldId);
    }
}
