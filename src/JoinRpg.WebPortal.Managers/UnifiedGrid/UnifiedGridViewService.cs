using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Web.Claims.UnifiedGrid;

namespace JoinRpg.WebPortal.Managers.UnifiedGrid;

internal class UnifiedGridViewService(
    ICurrentUserAccessor currentUserAccessor,
    ICaptainRulesRepository captainRulesRepository,
    IProjectMetadataRepository projectMetadataRepository,
    IUnifiedGridRepository unifiedGridRepository,
    IProjectRepository projectRepository) : IUnifiedGridClient
{
    async Task<IReadOnlyCollection<UgItemForCaptainViewModel>> IUnifiedGridClient.GetForCaptain(ProjectIdentification projectId, UgStatusFilterView filter)
    {
        var access = await captainRulesRepository.GetCaptainRules(projectId, currentUserAccessor.UserIdentification);
        if (access.Count == 0)
        {
            return [];
        }
        var groups = await projectRepository.LoadGroups([.. access.Select(x => x.CharacterGroup)]);
        var allGroups = groups.SelectMany(x => x.GetChildrenGroupsIdentificationRecursiveIncludingThis()).Distinct();

        var items = await unifiedGridRepository.GetByGroups(projectId, (UgStatusSpec)filter, [.. allGroups]);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        return [.. items.Select(claim => ItemBuilder.BuildItemForCaptain(claim, currentUserAccessor, projectInfo)).WhereNotNull()];
    }
}
