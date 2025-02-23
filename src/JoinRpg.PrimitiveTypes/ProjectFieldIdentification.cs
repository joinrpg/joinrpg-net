using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;
[method: JsonConstructor]

public record ProjectFieldIdentification(ProjectIdentification ProjectId, int ProjectFieldId) : IProjectEntityId
{
    public ProjectFieldIdentification(int ProjectId, int ProjectFieldId) : this(new ProjectIdentification(ProjectId), ProjectFieldId)
    {

    }
    int IProjectEntityId.Id => ProjectFieldId;

    public static ProjectFieldIdentification? FromOptional(ProjectIdentification? project, int? projectFieldId)
    {
        return (project, projectFieldId) switch
        {
            (_, null) => null,
            (null, _) => null,
            _ => new(project, projectFieldId.Value)
        };
    }

    public override string ToString() => $"ProjectFieldId({ProjectFieldId}, {ProjectId})";
}
