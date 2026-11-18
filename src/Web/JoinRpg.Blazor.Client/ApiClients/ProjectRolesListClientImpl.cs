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
        return await (await httpClient.GetAsync($"webapi/project-roles-list/getlist?projectId={projectId.Value}"))
            .ReadFromJsonOrThrowProblem<ProjectRolesListViewModel>();
    }

    public async Task<ProjectRolesList> GetById(ProjectRolesListIdentification id)
    {
        return await (await httpClient.GetAsync($"webapi/project-roles-list/getbyid?targetId={id}"))
            .ReadFromJsonOrThrowProblem<ProjectRolesList>();
    }

    public async Task Remove(ProjectRolesListIdentification id)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsJsonAsync($"webapi/project-roles-list/remove?projectId={id.ProjectId.Value}", id);
            await response.EnsureSuccessOrThrowProblem();
        }
        catch (Exception e) when (e is not ServerProblemException)
        {
            logger.LogError(e, "Error during access");
            throw;
        }
    }

    public async Task<ProjectRolesListViewModel> SetDefault(ProjectRolesListIdentification id)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsJsonAsync($"webapi/project-roles-list/setdefault?projectId={id.ProjectId.Value}", id);
            return await response.ReadFromJsonOrThrowProblem<ProjectRolesListViewModel>();
        }
        catch (Exception e) when (e is not ServerProblemException)
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
            return await response.ReadFromJsonOrThrowProblem<ProjectRolesListViewModel>();
        }
        catch (Exception e) when (e is not ServerProblemException)
        {
            logger.LogError(e, "Error during access");
            throw;
        }
    }

    public async Task<ProjectRolesListViewModel> Update(ProjectRolesList model)
    {
        var projectRolesListId = model.ProjectRolesListId
            ?? throw new ArgumentException("Нельзя сохранить ещё не созданную сетку ролей", nameof(model));
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsJsonAsync($"webapi/project-roles-list/update?projectId={projectRolesListId.ProjectId.Value}", model);
            return await response.ReadFromJsonOrThrowProblem<ProjectRolesListViewModel>();
        }
        catch (Exception e) when (e is not ServerProblemException)
        {
            logger.LogError(e, "Error during access");
            throw;
        }
    }
}
