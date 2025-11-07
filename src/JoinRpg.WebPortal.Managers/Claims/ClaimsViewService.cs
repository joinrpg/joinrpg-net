using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Claims;

namespace JoinRpg.WebPortal.Managers.Claims;
internal class ClaimsViewService(IClaimService claimService, IClaimsRepository claimsRepository, ICurrentUserAccessor currentUserAccessor) : IClaimClient
{
    public async Task AllowSensitiveData(ProjectIdentification projectId)
    {
        var claims = await claimsRepository.GetClaimsForPlayer(currentUserAccessor.UserIdentification, ClaimStatusSpec.Active);
        foreach (var claim in claims.Where(c => c.ProjectId == projectId))
        {
            await claimService.AllowSensitiveData(claim.GetId());
        }
    }

    async Task<IReadOnlyCollection<ClaimLinkViewModel>> IClaimClient.GetClaims(ProjectIdentification projectId, ClaimStatusSpec claimStatusSpec)
    => ToClaimViewModels(await claimsRepository.GetClaimHeadersWithPlayer(projectId, claimStatusSpec));

    private static IReadOnlyCollection<ClaimLinkViewModel> ToClaimViewModels(IReadOnlyCollection<Data.Interfaces.ClaimWithPlayer> claims)
        => [.. claims.Select(x => new ClaimLinkViewModel(x.ClaimId, x.Player.DisplayName, x.CharacterName, x.ExtraNicknames ?? ""))];
}
