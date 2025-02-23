using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
public record ProjectFieldVariantIdentification(ProjectFieldIdentification FieldId, int ProjectFieldVariantId) : IProjectEntityId
{
    public ProjectFieldVariantIdentification(int ProjectId, int ProjectFieldId, int ProjectFieldVariantId) : this(new ProjectFieldIdentification(ProjectId, ProjectFieldId), ProjectFieldVariantId)
    {

    }

    public ProjectFieldVariantIdentification(ProjectIdentification ProjectId, int ProjectFieldId, int ProjectFieldVariantId) : this(new ProjectFieldIdentification(ProjectId, ProjectFieldId), ProjectFieldVariantId)
    {

    }


    public ProjectIdentification ProjectId => FieldId.ProjectId;

    int IProjectEntityId.Id => ProjectFieldVariantId;

    public static ProjectFieldVariantIdentification? FromOptional(ProjectFieldIdentification? fieldId, int? projectFieldVariantId)
    {
        return (fieldId, projectFieldVariantId) switch
        {
            (_, null) => null,
            (null, _) => null,
            _ => new(fieldId, projectFieldVariantId.Value)
        };
    }
}
