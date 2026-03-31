using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
[ProjectEntityId]
public partial record ProjectFieldIdentification(ProjectIdentification ProjectId, int ProjectFieldId)
{
}
