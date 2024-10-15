namespace JoinRpg.PrimitiveTypes;

/// <summary>
/// Moniker that indetifies character and user that performs access to it.
/// </summary>
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
}
