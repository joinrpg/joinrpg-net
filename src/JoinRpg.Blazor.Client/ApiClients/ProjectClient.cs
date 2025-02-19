using System.Net.Http.Json;
using JoinRpg.Web.ProjectCommon.Projects;

namespace JoinRpg.Blazor.Client.ApiClients;

public class ProjectListClient(HttpClient httpClient) : IProjectListClient
{
    public async Task<ProjectDto[]> GetProjectsWithMyMasterAccess()
    {
        return await httpClient.GetFromJsonAsync<ProjectDto[]>($"webapi/projects/GetProjectsWithMyMasterAccess") ?? throw new Exception("Couldn't get result from server");
    }
}

public class ProjectCreateClient(HttpClient httpClient) : IProjectCreateClient
{
    public async Task<ProjectCreateResultViewModel> CreateProject(ProjectCreateViewModel model)
    {
        var response = await httpClient.PostAsJsonAsync("/webapi/project/create", model);
        return await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<ProjectCreateResultViewModel>() ?? throw new Exception("Couldn't get result from server");
    }
}
