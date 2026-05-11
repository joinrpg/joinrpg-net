using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectMasterTools.ProjectRolesLists;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class ProjectRolesListClientImpl(
    HttpClient httpClient,
    CsrfTokenProvider csrfTokenProvider,
    ILogger<ProjectRolesListClientImpl> logger) : IProjectRolesListClient
{
    public async Task<ProjectRolesListViewModel> GetList(ProjectIdentification projectId)
    {
        return await httpClient.GetFromJsonAsync<ProjectRolesListViewModel>(
             $"webapi/project-roles-list/getlist?projectId={projectId.Value}")
             ?? throw new Exception("Couldn't get result from server");
    }

    public async Task Remove(ProjectRolesListIdentification id)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsJsonAsync($"webapi/project-roles-list/remove?projectId={id.ProjectId.Value}", id);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during access");
            throw;
        }
    }

    public async Task<ProjectRolesListViewModel> Create(ProjectIdentification projectId, AddProjectRolesListViewModel model)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsJsonAsync($"webapi/project-roles-list/create?projectId={projectId.Value}", model);
            return await response
                .EnsureSuccessStatusCode()
                .Content
                .ReadFromJsonAsync<ProjectRolesListViewModel>()
                ?? throw new Exception("Empty");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during access");
            throw;
        }
    }

    public async Task<ProjectRolesListViewModel> Update(DomainTypes.ProjectMetadata.ProjectRolesList model)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsJsonAsync($"webapi/project-roles-list/update?projectId={model.ProjectRolesListId.ProjectId.Value}", model);
            return await response
                .EnsureSuccessStatusCode()
                .Content
                .ReadFromJsonAsync<ProjectRolesListViewModel>()
                ?? throw new Exception("Empty");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during access");
            throw;
        }
    }
}
