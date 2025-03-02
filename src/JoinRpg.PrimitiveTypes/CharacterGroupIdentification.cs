using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
public record CharacterGroupIdentification(
    ProjectIdentification ProjectId,
    int CharacterGroupId) : IProjectEntityId
{

    public CharacterGroupIdentification(int ProjectId, int CharacterGroupId) : this(new ProjectIdentification(ProjectId), CharacterGroupId)
    {

    }
    int IProjectEntityId.Id => CharacterGroupId;

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

    public static IEnumerable<CharacterGroupIdentification> FromList(IEnumerable<int> list, ProjectIdentification projectId) => list.Select(g => new CharacterGroupIdentification(projectId, g));
}
