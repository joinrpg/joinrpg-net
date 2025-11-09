using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Claims;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.Web.ProjectCommon.Claims;

namespace JoinRpg.WebPortal.Managers.Claims;
internal class ClaimsViewService(
    IClaimService claimService,
    IClaimsRepository claimsRepository,
    ICurrentUserAccessor currentUserAccessor,
    ICaptainRulesRepository captainRulesRepository,
    IProjectMetadataRepository projectMetadataRepository,
    IProjectRepository projectRepository) : IClaimListClient, IClaimOperationClient, IClaimGridClient
{
    public async Task AllowSensitiveData(ProjectIdentification projectId)
    {
        var claims = await claimsRepository.GetClaimsForPlayer(currentUserAccessor.UserIdentification, ClaimStatusSpec.Active);
        foreach (var claim in claims.Where(c => c.ProjectId == projectId))
        {
            await claimService.AllowSensitiveData(claim.GetId());
        }
    }

    async Task<IReadOnlyCollection<ClaimLinkViewModel>> IClaimListClient.GetClaims(ProjectIdentification projectId, ClaimStatusSpec claimStatusSpec)
    => (await claimsRepository.GetClaimHeadersWithPlayer(projectId, claimStatusSpec)).ToClaimViewModels();
    async Task<IReadOnlyCollection<ClaimListItemForCaptainViewModel>> IClaimGridClient.GetForCaptain(ProjectIdentification projectId, ClaimStatusSpec claimStatusSpec)
    {
        var access = await captainRulesRepository.GetCaptainRules(projectId, currentUserAccessor.UserIdentification);
        if (access.Count == 0)
        {
            return [];
        }
        var groups = await projectRepository.LoadGroups([.. access.Select(x => x.CharacterGroup)]);
        var allGroups = groups.SelectMany(x => x.GetChildrenGroupsIdRecursiveIncludingThis()).Distinct();

        var claims = await claimsRepository.GetClaimsForGroups(projectId, claimStatusSpec, [.. allGroups]);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        return [.. claims.Select(claim => ClaimListBuilder.BuildItemForCaptain(claim, currentUserAccessor, projectInfo)).WhereNotNull()];
    }
}

public static class Builders
{
    public static IReadOnlyCollection<ClaimLinkViewModel> ToClaimViewModels(this IReadOnlyCollection<Data.Interfaces.ClaimWithPlayer> claims)
    => [.. claims.Select(x => new ClaimLinkViewModel(x.ClaimId, x.Player.DisplayName, x.CharacterName, x.ExtraNicknames ?? "", x.Player.UserId))];
}
