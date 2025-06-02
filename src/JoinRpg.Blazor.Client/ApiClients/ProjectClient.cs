using System.Net.Http.Json;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Web.ProjectCommon.Projects;
using JoinRpg.Web.ProjectMasterTools.Settings;

namespace JoinRpg.Blazor.Client.ApiClients;

public class ProjectListClient(HttpClient httpClient) : IProjectListClient
{
    public async Task<ProjectDto[]> GetProjects(ProjectSelectionCriteria projectSelectionCriteria)
    {
        return await httpClient.GetFromJsonAsync<ProjectDto[]>($"webapi/projects/GetProjects?criteria={projectSelectionCriteria}") ?? throw new Exception("Couldn't get result from server");
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
}
