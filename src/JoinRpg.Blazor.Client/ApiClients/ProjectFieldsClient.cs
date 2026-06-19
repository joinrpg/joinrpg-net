using JoinRpg.Web.ProjectCommon.Fields;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class ProjectFieldsClient(HttpClient httpClient) : IProjectFieldsClient
{
    public async Task<List<ProjectFieldDto>> GetProjectFields(ProjectIdentification projectId)
    {
        return await httpClient.GetFromJsonAsync<List<ProjectFieldDto>>(
            $"/webapi/project-fields/GetProjectFields?projectId={projectId.Value}")
            ?? throw new Exception("Couldn't get result from server");
    }
}
