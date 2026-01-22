using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.Web.Claims;

namespace JoinRpg.Web.Models.Claims;

public static class ClaimStatusBuilders
{
    public static ClaimFullStatusView CreateFullStatus(Claim claim, AccessArguments accessArguments)
    {
        if (accessArguments.CanViewDenialStatus)
        {
            return new ClaimFullStatusView((ClaimStatusView)claim.ClaimStatus, (ClaimDenialStatusView?)claim.ClaimDenialStatus);
        }
        else
        {
            return new ClaimFullStatusView((ClaimStatusView)claim.ClaimStatus, null);
        }
    }
}
