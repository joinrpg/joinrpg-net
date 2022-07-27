using JoinRpg.DataModel;

namespace JoinRpg.Domain;

public static class AccessArgumentsFactory
{
    public static AccessArguments Create(Character character, int? userId)
    {
        ArgumentNullException.ThrowIfNull(character);

        return new AccessArguments(
            MasterAccess: character.HasMasterAccess(userId),
            PlayerAccessToCharacter: character.HasPlayerAccess(userId),
            PlayerAccesToClaim: character.ApprovedClaim?.HasPlayerAccesToClaim(userId) ?? false);
    }

    public static AccessArguments Create(Claim claim, int? userId)
    {
        ArgumentNullException.ThrowIfNull(claim);

        return new AccessArguments(
            MasterAccess: claim.HasMasterAccess(userId),
            PlayerAccessToCharacter: claim.Character?.HasPlayerAccess(userId) ?? false,
            PlayerAccesToClaim: claim.HasPlayerAccesToClaim(userId));
    }
}
