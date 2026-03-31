using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
[ProjectEntityId]
public partial record ProjectFieldVariantIdentification(ProjectFieldIdentification FieldId, int ProjectFieldVariantId)
{
}
