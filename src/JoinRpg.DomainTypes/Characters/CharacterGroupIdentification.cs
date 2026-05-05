using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes;

[method: JsonConstructor]
[TypedEntityId]
public partial record CharacterGroupIdentification(
    ProjectIdentification ProjectId,
    int CharacterGroupId) : IProjectEntityId
{
}
