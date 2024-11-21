namespace JoinRpg.PrimitiveTypes;

public record class AccessArguments(
        /// <summary>
        /// true if a user is logged in, he is a master of this game, he has access to the character or claim
        /// </summary>
        bool MasterAccess,
        /// <summary>
        /// true if a user is logged in and he is assigned with the character
        /// </summary>
        bool PlayerAccessToCharacter,
        /// <summary>
        /// true if a user is logged in and he is the owner of the claim
        /// </summary>
        bool PlayerAccesToClaim,
        bool EditAllowed)
{

    /// <summary>
    /// true if there is master or player access to the character
    /// </summary>
    public bool AnyAccessToCharacter { get; } = MasterAccess || PlayerAccessToCharacter;

    /// <summary>
    /// true, if there is master or player access to the claim
    /// </summary>
    public bool AnyAccessToClaim { get; } = PlayerAccesToClaim || PlayerAccesToClaim;

    public AccessArguments WithoutMasterAccess() => this with { MasterAccess = false };

    public override string ToString() => $"AccessArguments(MasterAccess:{MasterAccess}, CharacterAccess:{PlayerAccessToCharacter}, PlayerAccesToClaim:{PlayerAccesToClaim}";
}
