using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
[TypedEntityId]
public partial record CharacterIdentification(
    ProjectIdentification ProjectId,
    int CharacterId)
{
}
