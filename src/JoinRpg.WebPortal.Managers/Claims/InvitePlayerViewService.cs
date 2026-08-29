using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.ProjectCommon.Claims;

namespace JoinRpg.WebPortal.Managers.Claims;

internal class InvitePlayerViewService(
    IClaimService claimService,
    IUserLinkResolver userLinkResolver,
    IProjectMetadataRepository projectMetadataRepository)
    : IInvitePlayerClient
{
    public async Task<ClaimIdentification> InvitePlayer(CharacterIdentification characterId, string userLink, string claimText)
    {
        var userId = await userLinkResolver.ResolveAsync(userLink);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(characterId.ProjectId);
        return await claimService.AddClaimFromMaster(
            characterId, userId, claimText, FieldLayerContainer.Empty(projectInfo));
    }
}
