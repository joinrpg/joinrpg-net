using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
[TypedEntityId]
public partial record struct ProjectFieldIdentification(ProjectIdentification ProjectId, int ProjectFieldId) : IProjectEntityId
{
}
