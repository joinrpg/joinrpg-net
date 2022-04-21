namespace JoinRpg.PrimitiveTypes;

/// <summary>
/// Moniker that indetifies character and user that performs access to it.
/// </summary>
public record CharacterIdentification(
    int ProjectId,
    int CharacterId)
{ }
