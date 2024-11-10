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

    public int RootCharacterGroupId { get; }

    public ProjectInfo(
        ProjectIdentification projectId,
        string projectName,
        string ordering,
        IReadOnlyCollection<ProjectFieldInfo> fields,
        ProjectFieldSettings projectFieldSettings,
        ProjectFinanceSettings projectFinanceSettings,
        bool accomodationEnabled,
        CharacterIdentification? defaultTemplateCharacter,
        bool allowToSetGroups,
        int rootCharacterGroupId)
    {
        UnsortedFields = fields;
        ProjectId = projectId;
        ProjectName = projectName;
        sortedFieldsContainer = VirtualOrderContainerFacade.CreateLazy(fields, ordering);
        sortedActiveFieldsContainer = VirtualOrderContainerFacade.CreateLazy(fields.Where(f => f.IsActive), ordering);
        ProjectFieldSettings = projectFieldSettings;
        ProjectFinanceSettings = projectFinanceSettings;
        AccomodationEnabled = accomodationEnabled;
        if (projectFieldSettings.NameField is ProjectFieldIdentification nameField)
        {
            CharacterNameField = GetFieldById(nameField);
        }

        if (projectFieldSettings.DescriptionField is ProjectFieldIdentification descriptionField)
        {
            CharacterDescriptionField = GetFieldById(descriptionField);
        }

        TimeSlotField = UnsortedFields.SingleOrDefault(f => f.Type == ProjectFieldType.ScheduleTimeSlotField && f.IsActive);
        RoomField = UnsortedFields.SingleOrDefault(f => f.Type == ProjectFieldType.ScheduleRoomField && f.IsActive);

        DefaultTemplateCharacter = defaultTemplateCharacter;
        AllowToSetGroups = allowToSetGroups;
        RootCharacterGroupId = rootCharacterGroupId;
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
