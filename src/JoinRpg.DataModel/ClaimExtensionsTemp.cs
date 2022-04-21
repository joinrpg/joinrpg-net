namespace JoinRpg.DataModel;

/// <summary>
/// TODO: Move this down and merge with predicates
/// </summary>
public static class ClaimExtensionsTemp
{
    public static bool IsActive(this Claim.Status claimStatus) => claimStatus != Claim.Status.DeclinedByMaster
                                                                  && claimStatus != Claim.Status.DeclinedByUser
                                                                  && claimStatus != Claim.Status.OnHold;
}
