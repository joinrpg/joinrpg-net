using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
[ProjectEntityId]
public partial record CharacterGroupIdentification(
    ProjectIdentification ProjectId,
    int CharacterGroupId)
{
}
