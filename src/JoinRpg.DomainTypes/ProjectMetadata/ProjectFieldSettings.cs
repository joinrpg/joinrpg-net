namespace JoinRpg.DomainTypes.ProjectMetadata;

public record class ProjectFieldSettings(
    ProjectFieldIdentification? NameField,
    ProjectFieldIdentification? DescriptionField)
{
}
