namespace JoinRpg.PrimitiveTypes;

public record ProjectFieldIdentification(ProjectIdentification ProjectId, int ProjectFieldId)
{
    public static ProjectFieldIdentification? FromOptional(ProjectIdentification? project, int? projectFieldId)
    {
        return (project, projectFieldId) switch
        {
            (_, null) => null,
            (null, _) => null,
            _ => new(project, projectFieldId.Value)
        };
    }
}
