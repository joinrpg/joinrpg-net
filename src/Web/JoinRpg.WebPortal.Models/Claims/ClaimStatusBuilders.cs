using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Web.Claims;

namespace JoinRpg.Web.Models.Claims;

public static class ClaimStatusBuilders
{
    /// <summary>
    /// То же, что для EF-сущности, но поверх доменного агрегата (ADR013).
    /// </summary>
    public static ClaimFullStatusView CreateFullStatus(CharacterClaimInfo claim, AccessArguments accessArguments)
        => new(
            (ClaimStatusView)claim.Status,
            accessArguments.CanViewDenialStatus ? (ClaimDenialStatusView?)claim.DenialStatus : null);

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
