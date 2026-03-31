using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
[ProjectEntityId]
public partial record CharacterIdentification(
    ProjectIdentification ProjectId,
    int CharacterId)
{
}
