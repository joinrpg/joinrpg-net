namespace JoinRpg.PrimitiveTypes;

public record ProjectFieldVariantIdentification(ProjectFieldIdentification FieldId, int ProjectFieldVariantId)
{
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
