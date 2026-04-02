using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
[TypedEntityId]
public partial record ProjectFieldIdentification(ProjectIdentification ProjectId, int ProjectFieldId) : IProjectEntityId
{
}
