using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes;

[method: JsonConstructor]
[TypedEntityId]
public partial record ProjectFieldIdentification(ProjectIdentification ProjectId, int ProjectFieldId) : IProjectEntityId
{
}
