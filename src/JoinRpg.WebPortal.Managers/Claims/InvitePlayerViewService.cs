using JoinRpg.Services.Interfaces;
using JoinRpg.Web.ProjectCommon.Claims;

namespace JoinRpg.WebPortal.Managers.Claims;

internal class InvitePlayerViewService(
    IClaimService claimService)
    : IInvitePlayerClient
{
    public async Task<ClaimIdentification> InvitePlayer(CharacterIdentification characterId, string userLink, string claimText)
    {
        var userId = UserLinkParser.ParseUserLink(userLink);
        return await claimService.AddClaimFromMaster(characterId, new UserIdentification(userId), claimText, new Dictionary<int, string?>());
    }
}
