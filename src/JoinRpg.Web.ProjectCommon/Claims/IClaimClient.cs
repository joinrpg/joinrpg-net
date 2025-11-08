using JoinRpg.PrimitiveTypes.Claims;

namespace JoinRpg.Web.ProjectCommon.Claims;
public interface IClaimListClient
{
    Task<IReadOnlyCollection<ClaimLinkViewModel>> GetClaims(ProjectIdentification projectId, ClaimStatusSpec claimStatusSpec);
}
