using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class RolesListSelectorClientImpl(HttpClient httpClient) : IRolesListSelectorClient
{
    public async Task<ProjectRolesListDto[]> GetRolesListsList(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/project-roles-list/getroleslistslist?projectId={projectId.Value}"))
            .ReadFromJsonOrThrowProblem<ProjectRolesListDto[]>();
}
