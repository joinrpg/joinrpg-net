using JoinRpg.Services.Interfaces;
using JoinRpg.Web.ProjectCommon.Claims;

namespace JoinRpg.WebPortal.Managers.Claims;

internal class InvitePlayerViewService(
    IClaimService claimService,
    IUserLinkResolver userLinkResolver)
    : IInvitePlayerClient
{
    public async Task<ClaimIdentification> InvitePlayer(CharacterIdentification characterId, string userLink, string claimText)
    {
        var userId = await userLinkResolver.ResolveAsync(userLink);
        return await claimService.AddClaimFromMaster(characterId, userId, claimText, new Dictionary<int, string?>());
    }
}
