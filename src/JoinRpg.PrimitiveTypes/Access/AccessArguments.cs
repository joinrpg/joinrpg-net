namespace JoinRpg.PrimitiveTypes.Access;
/// <summary>
/// 
/// </summary>
/// <param name="MasterAccess">true if a user is logged in, he is a master of this game, he has access to the character or claim</param>
/// <param name="PlayerAccessToCharacter">true if a user is logged in and he is assigned with the character</param>
/// <param name="PlayerAccesToClaim"> true if a user is logged in and he is the owner of the claim</param>
/// <param name="EditAllowed"></param>
/// <param name="Published"></param>
/// <param name="CharacterPublic"></param>
/// <param name="IsCapitan"></param>
public record class AccessArguments(
    bool MasterAccess,
    bool PlayerAccessToCharacter,
    bool PlayerAccesToClaim,
    bool EditAllowed,
    bool Published,
    bool CharacterPublic,
    bool IsCapitan)
{

    /// <summary>
    /// true if there is master or player access to the character
    /// </summary>
    public bool AnyAccessToCharacter => MasterAccess || PlayerAccessToCharacter;

    public bool CanViewCharacterName => MasterAccess || PlayerAccessToCharacter || CharacterPublic || IsCapitan;

    /// <summary>
    /// true if the character page is accessible to the current user at all.
    /// A character page is visible if:
    /// - the character is public (<see cref="CharacterPublic"/>), OR
    /// - the viewer has master access to the project (<see cref="MasterAccess"/>), OR
    /// - the viewer is the approved player for this character (<see cref="PlayerAccessToCharacter"/>), OR
    /// - the project has published plots, which opens all characters to readers (<see cref="Published"/>).
    /// </summary>
    public bool CanViewCharacterAtAll => CharacterPublic || MasterAccess || PlayerAccessToCharacter || Published;

    /// <summary>
    /// true, if there is master or player access to the claim
    /// </summary>
    public bool AnyAccessToClaim => PlayerAccesToClaim || MasterAccess;

    public bool CharacterPlotAccess => Published || PlayerAccessToCharacter || MasterAccess;
    public static AccessArguments None { get; } = new AccessArguments(false, false, false, false, false, false, false);
    public bool CanViewDenialStatus => MasterAccess;

    public AccessArguments WithoutMasterAccess() => this with { MasterAccess = false };

    public override string ToString() => $"AccessArguments(MasterAccess:{MasterAccess}, CharacterAccess:{PlayerAccessToCharacter}, PlayerAccesToClaim:{PlayerAccesToClaim}";
}
