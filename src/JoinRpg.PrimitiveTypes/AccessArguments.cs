namespace JoinRpg.Domain;

public record class AccessArguments(
        bool MasterAccess,
        bool PlayerAccessToCharacter,
        bool PlayerAccesToClaim)
{
    /// <summary>
    /// true if a user is logged in, he is a master of this game, he has access to the character or claim
    /// </summary>
    public bool MasterAccess { get; } = MasterAccess;

    /// <summary>
    /// true if a user is logged in and he is assigned with the character
    /// </summary>
    public bool PlayerAccessToCharacter { get; } = PlayerAccessToCharacter;

    /// <summary>
    /// true if a user is logged in and he is the owner of the claim
    /// </summary>
    public bool PlayerAccesToClaim { get; } = PlayerAccesToClaim;

    /// <summary>
    /// true if there is master or player access to the character
    /// </summary>
    public bool AnyAccessToCharacter { get; } = MasterAccess || PlayerAccessToCharacter;

    /// <summary>
    /// true, if there is master or player access to the claim
    /// </summary>
    public bool AnyAccessToClaim { get; } = PlayerAccesToClaim || PlayerAccesToClaim;

    public override string ToString() => $"AccessArguments(MasterAccess:{MasterAccess}, CharacterAccess:{PlayerAccessToCharacter}, PlayerAccesToClaim:{PlayerAccesToClaim}";
}
