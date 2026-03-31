using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
[ProjectEntityId]
public partial record CharacterGroupIdentification(
    ProjectIdentification ProjectId,
    int CharacterGroupId)
{
    public static CharacterGroupIdentification? FromOptional(int ProjectId, int? CharacterGroupId)
    {
        if (CharacterGroupId is null || CharacterGroupId == -1)
        {
            return null;
        }
        else
        {
            return new CharacterGroupIdentification(new ProjectIdentification(ProjectId), CharacterGroupId.Value);
        }
    }
}
