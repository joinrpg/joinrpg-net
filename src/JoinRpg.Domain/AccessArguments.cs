using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain;

public class AccessArguments
{
    public AccessArguments(bool masterAccess,
        bool playerAccessToCharacter,
        bool playerAccesToClaim)
    {
        MasterAccess = masterAccess;
        PlayerAccessToCharacter = playerAccessToCharacter;
        PlayerAccesToClaim = playerAccesToClaim;
    }

    public AccessArguments([NotNull] Character character, int? userId)
    {
        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }

        MasterAccess = character.HasMasterAccess(userId);
        PlayerAccessToCharacter = character.HasPlayerAccess(userId);
        PlayerAccesToClaim = character.ApprovedClaim?.HasPlayerAccesToClaim(userId) ?? false;
    }

    public AccessArguments([NotNull] Claim claim, int? userId)
    {
        if (claim == null)
        {
            throw new ArgumentNullException(nameof(claim));
        }

        MasterAccess = claim.HasMasterAccess(userId);
        PlayerAccessToCharacter = claim.Character?.HasPlayerAccess(userId) ?? false;
        PlayerAccesToClaim = claim.HasPlayerAccesToClaim(userId);
    }

    /// <summary>
    /// true if a user is logged in, he is a master of this game, he has access to the character or claim
    /// </summary>
    public bool MasterAccess { get; }

    /// <summary>
    /// true if a user is logged in and he is assigned with the character
    /// </summary>
    public bool PlayerAccessToCharacter { get; }

    /// <summary>
    /// true if a user is logged in and he is the owner of the claim
    /// </summary>
    public bool PlayerAccesToClaim { get; }

    /// <summary>
    /// true if there is master or player access to the character
    /// </summary>
    public bool AnyAccessToCharacter => MasterAccess || PlayerAccessToCharacter;

    /// <summary>
    /// true, if there is master or player access to the claim
    /// </summary>
    public bool AnyAccessToClaim => PlayerAccesToClaim || PlayerAccesToClaim;

    public override string ToString() => $"AccessArguments(MasterAccess:{MasterAccess}, CharacterAccess:{PlayerAccessToCharacter}, PlayerAccesToClaim:{PlayerAccesToClaim}";
}
