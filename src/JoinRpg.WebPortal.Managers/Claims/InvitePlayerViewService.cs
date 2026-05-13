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
        var text = string.IsNullOrWhiteSpace(claimText)
            ? "Мастер пригласил вас на роль"
            : claimText;
        return await claimService.AddClaimFromMaster(characterId, new UserIdentification(userId), text, new Dictionary<int, string?>());
    }
}
