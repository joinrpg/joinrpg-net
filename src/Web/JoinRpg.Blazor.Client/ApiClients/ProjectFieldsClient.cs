using JoinRpg.Web.ProjectCommon.Fields;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class ProjectFieldsClient(HttpClient httpClient) : IProjectFieldsClient
{
    public async Task<List<ProjectFieldDto>> GetProjectFields(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"/webapi/project-fields/GetProjectFields?projectId={projectId.Value}"))
            .ReadFromJsonOrThrowProblem<List<ProjectFieldDto>>();
}
