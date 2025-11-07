using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Claims;

namespace JoinRpg.Web.Claims;
public interface IClaimClient
{
    Task AllowSensitiveData(ProjectIdentification projectId);

    Task<IReadOnlyCollection<ClaimLinkViewModel>> GetClaims(ProjectIdentification projectId, ClaimStatusSpec claimStatusSpec);
}
