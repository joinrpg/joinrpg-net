namespace JoinRpg.PrimitiveTypes;
public record CharacterIdentification(
    int ProjectId,
    int CharacterId)
{

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
}
