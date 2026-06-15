using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.ProjectMetadata;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Web.ProjectMasterTools.ProjectRolesLists;

namespace JoinRpg.WebPortal.Managers.ProjectMasterTools.ProjectRolesLists;

internal class ProjectRolesListViewService(
    IProjectRolesListRepository repository,
    IProjectRolesListService service,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository)
    : IProjectRolesListClient
{
    public async Task<ProjectRolesListViewModel> GetList(ProjectIdentification projectId)
    {
        var domainItems = await repository.GetForProjectAsync(projectId);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var groupIdentifications = domainItems
            .Where(item => item.CharacterGroupId != null)
            .Select(item => item.CharacterGroupId!)
            .Distinct()
            .ToList();

        IReadOnlyDictionary<CharacterGroupIdentification, string>? groupNames = null;
        if (groupIdentifications.Count > 0)
        {
            groupNames = projectInfo.GetGroupsById(groupIdentifications)
                .ToDictionary(g => g.Id, g => g.Name);
        }

        return ProjectRolesListViewModelBuilder.Build(
            domainItems,
            projectInfo,
            currentUserAccessor,
            groupNames);
    }

    public async Task<ProjectRolesList> GetById(ProjectRolesListIdentification id)
    {
        return await service.GetByIdAsync(id);
    }

    public async Task Remove(ProjectRolesListIdentification id)
    {
        await service.RemoveAsync(id);
    }

    public async Task<ProjectRolesListViewModel> Create(ProjectIdentification projectId, AddProjectRolesListViewModel model)
    {
        var domainModel = model.ToDomain(projectId, -1);
        await service.CreateAsync(domainModel);
        return await GetList(projectId);
    }

    public async Task<ProjectRolesListViewModel> Update(DomainTypes.ProjectMetadata.ProjectRolesList model)
    {
        await service.UpdateAsync(model);
        return await GetList(model.ProjectRolesListId.ProjectId);
    }
}
