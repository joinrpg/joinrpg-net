using System.Net.Http.Json;
using JoinRpg.Web.ProjectCommon.Projects;

namespace JoinRpg.Blazor.Client.ApiClients;

public class ProjectClient(HttpClient httpClient) : IProjectListClient
{
    public async Task<ProjectDto[]> GetProjectsWithMyMasterAccess()
    {
        return await httpClient.GetFromJsonAsync<ProjectDto[]>($"webapi/projects/GetProjectsWithMyMasterAccess") ?? throw new Exception("Couldn't get result from server");
    }
}
