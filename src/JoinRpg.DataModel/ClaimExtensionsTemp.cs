using JoinRpg.PrimitiveTypes.Claims;

namespace JoinRpg.DataModel;

/// <summary>
/// TODO: Move this down and merge with predicates
/// </summary>
public static class ClaimExtensionsTemp
{
    public static bool IsActive(this ClaimStatus claimStatus) => claimStatus != ClaimStatus.DeclinedByMaster
                                                                  && claimStatus != ClaimStatus.DeclinedByUser
                                                                  && claimStatus != ClaimStatus.OnHold;
}
