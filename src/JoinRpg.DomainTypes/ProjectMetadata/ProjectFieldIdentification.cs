using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes;

[method: JsonConstructor]
[TypedEntityId]
public partial record ProjectFieldIdentification(ProjectIdentification ProjectId, int ProjectFieldId) : IProjectEntityId
{
}
