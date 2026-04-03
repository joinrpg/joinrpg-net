using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
[TypedEntityId]
public partial record struct CharacterGroupIdentification(
    ProjectIdentification ProjectId,
    int CharacterGroupId) : IProjectEntityId
{
}
