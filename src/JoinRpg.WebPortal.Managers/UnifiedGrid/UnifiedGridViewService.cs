using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Web.Claims.UnifiedGrid;

namespace JoinRpg.WebPortal.Managers.UnifiedGrid;

internal class UnifiedGridViewService(
    ICurrentUserAccessor currentUserAccessor,
    ICaptainRulesRepository captainRulesRepository,
    IProjectMetadataRepository projectMetadataRepository,
    IUnifiedGridRepository unifiedGridRepository) : IUnifiedGridClient
{
    async Task<IReadOnlyCollection<UgItemForCaptainViewModel>> IUnifiedGridClient.GetForCaptain(ProjectIdentification projectId, UgStatusFilterView filter)
    {
        var access = await captainRulesRepository.GetCaptainRules(projectId, currentUserAccessor.UserIdentification);
        if (access.Count == 0)
        {
            return [];
        }
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var allGroups = projectInfo.GetChildGroupIdsIncludingThis([.. access.Select(x => x.CharacterGroup)]);

        var items = await unifiedGridRepository.GetByGroups(projectId, (UgStatusSpec)filter, [.. allGroups]);

        return [.. items.Select(claim => ItemBuilder.BuildItemForCaptain(claim, currentUserAccessor, projectInfo)).WhereNotNull()];
    }
}
