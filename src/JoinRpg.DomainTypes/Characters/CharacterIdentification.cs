using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes;

[method: JsonConstructor]
[TypedEntityId]
public partial record CharacterIdentification(
    ProjectIdentification ProjectId,
    int CharacterId) : IProjectEntityId
{
}
