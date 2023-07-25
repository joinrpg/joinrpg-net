using JoinRpg.Helpers;

namespace JoinRpg.PrimitiveTypes.ProjectMetadata;
public record class ProjectInfo
{
    private readonly Lazy<VirtualOrderContainer<ProjectFieldInfo>> container;

    public IReadOnlyList<ProjectFieldInfo> SortedFields => container.Value.OrderedItems;

    public ProjectIdentification ProjectId { get; }
    public string ProjectName { get; }
    public IReadOnlyCollection<ProjectFieldInfo> UnsortedFields { get; }
    public ProjectFieldInfo? CharacterNameField { get; }
    public ProjectFieldInfo? CharacterDescriptionField { get; }

    public ProjectFieldSettings ProjectFieldSettings { get; }

    public ProjectInfo(
        ProjectIdentification projectId,
        string projectName,
        string ordering,
        IReadOnlyCollection<ProjectFieldInfo> fields,
        ProjectFieldSettings projectFieldSettings
        )
    {
        UnsortedFields = fields;
        ProjectId = projectId;
        ProjectName = projectName;
        container = VirtualOrderContainerFacade.CreateLazy(fields, ordering);
        ProjectFieldSettings = projectFieldSettings;

        if (projectFieldSettings.NameField is ProjectFieldIdentification nameField)
        {
            CharacterNameField = GetFieldById(nameField);
        }

        if (projectFieldSettings.DescriptionField is ProjectFieldIdentification descriptionField)
        {
            CharacterDescriptionField = GetFieldById(descriptionField);
        }
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
