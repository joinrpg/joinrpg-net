using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes;

[method: JsonConstructor]
[TypedEntityId]
public partial record CharacterGroupIdentification(
    ProjectIdentification ProjectId,
    int CharacterGroupId) : IProjectEntityId
{
}
