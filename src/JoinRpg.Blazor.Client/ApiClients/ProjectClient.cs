using JoinRpg.Web.AdminTools;
using JoinRpg.Web.Games.Projects;
using JoinRpg.Web.ProjectCommon.Projects;
using JoinRpg.Web.ProjectMasterTools.Settings;

namespace JoinRpg.Blazor.Client.ApiClients;

public class ProjectListClient(HttpClient httpClient) : IProjectListClient, IProjectListForAdminClient
{
    public async Task<List<ProjectLinkViewModel>> GetProjects(ProjectSelectionCriteria projectSelectionCriteria)
    {
        return await httpClient.GetFromJsonAsync<List<ProjectLinkViewModel>>($"webapi/projects/GetProjects?criteria={projectSelectionCriteria}") ?? throw new Exception("Couldn't get result from server");
    }

    public async Task<List<ProjectAdminListItemViewModel>> GetProjectsForAdmin(ProjectSelectionCriteria projectSelectionCriteria)
    {
        return await httpClient.GetFromJsonAsync<List<ProjectAdminListItemViewModel>>($"webapi/projects/GetProjectsForAdmin?criteria={projectSelectionCriteria}") ?? throw new Exception("Couldn't get result from server");
    }
}

public class ProjectCreateClient(HttpClient httpClient) : IProjectCreateClient
{
    public async Task<ProjectCreateResultViewModel> CreateProject(ProjectCreateViewModel model)
    {
        var response = await httpClient.PostAsJsonAsync("/webapi/project-create/create", model);
        return await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<ProjectCreateResultViewModel>() ?? throw new Exception("Couldn't get result from server");
    }
}

public class ProjectSettingsClient(HttpClient httpClient) : IProjectSettingsClient
{
    async Task<ProjectPublishSettingsViewModel> IProjectSettingsClient.GetPublishSettings(ProjectIdentification projectId)
        => await httpClient.GetFromJsonAsync<ProjectPublishSettingsViewModel>($"webapi/{projectId.Value}/project/GetPublishSettings") ?? throw new Exception("Couldn't get result from server");
    async Task IProjectSettingsClient.SavePublishSettings(ProjectPublishSettingsViewModel model)
    {
        var response = await httpClient.PostAsJsonAsync($"/webapi/{model.ProjectId.Value}/project/SavePublishSettings", model);
        response.EnsureSuccessStatusCode();
    }

    async Task<ProjectContactsSettingsViewModel> IProjectSettingsClient.GetContactSettings(ProjectIdentification projectId)
    => await httpClient.GetFromJsonAsync<ProjectContactsSettingsViewModel>($"webapi/{projectId.Value}/project/GetContactSettings") ?? throw new Exception("Couldn't get result from server");
    async Task IProjectSettingsClient.SaveContactSettings(ProjectContactsSettingsViewModel model)
    {
        var response = await httpClient.PostAsJsonAsync($"/webapi/{model.ProjectId.Value}/project/SaveContactSettings", model);
        response.EnsureSuccessStatusCode();
    }

    async Task IProjectSettingsClient.SaveClaimSettings(ProjectClaimSettingsViewModel model)
    {
        var response = await httpClient.PostAsJsonAsync($"/webapi/{model.ProjectId.Value}/project/SaveClaimSettings", model);
        response.EnsureSuccessStatusCode();
    }
    async Task<ProjectClaimSettingsViewModel> IProjectSettingsClient.GetClaimSettings(ProjectIdentification projectId)
        => await httpClient.GetFromJsonAsync<ProjectClaimSettingsViewModel>($"webapi/{projectId.Value}/project/GetClaimSettings") ?? throw new Exception("Couldn't get result from server");
}

public class ProjectInfoClient(HttpClient httpClient) : IProjectInfoClient
{
    async Task<ProjectInfoViewModel> IProjectInfoClient.GetProjectInfo(ProjectIdentification projectId)
        => await httpClient.GetFromJsonAsync<ProjectInfoViewModel>($"webapi/{projectId.Value}/project-info/GetProjectInfo") ?? throw new Exception("Couldn't get result from server");
}
