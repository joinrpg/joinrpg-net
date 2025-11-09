using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Claims;

namespace JoinRpg.Web.Claims;
public interface IClaimGridClient
{
    Task<IReadOnlyCollection<ClaimListItemForCaptainViewModel>> GetForCaptain(ProjectIdentification projectId, ClaimStatusSpec claimStatusSpec);
}
