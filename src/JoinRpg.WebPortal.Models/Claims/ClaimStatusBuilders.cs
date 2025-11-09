using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.Web.Claims;

namespace JoinRpg.Web.Models.Claims;
internal static class ClaimStatusBuilders
{
    internal static ClaimFullStatusView CreateFullStatus(Claim claim, AccessArguments accessArguments)
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
