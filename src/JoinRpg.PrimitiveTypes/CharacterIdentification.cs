using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;
[method: JsonConstructor]
public record CharacterIdentification(
    ProjectIdentification ProjectId,
    int CharacterId) : IProjectEntityId
{
    public int Id => CharacterId;

    public CharacterIdentification(int projectId, int characterId) : this(new(projectId), characterId)
    {

    }

    public static CharacterIdentification? FromOptional(int ProjectId, int? CharacterId)
    {
        if (CharacterId is null || CharacterId == -1)
        {
            return null;
        }
        else
        {
            return new CharacterIdentification(ProjectId, CharacterId.Value);
        }
    }

    public static IEnumerable<CharacterIdentification> FromList(IEnumerable<int> list, ProjectIdentification projectId) => list.Select(g => new CharacterIdentification(projectId, g));

    public override string ToString() => $"CharacterId({CharacterId}, {ProjectId})";
}
