using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class ProjectRoleGridClientImpl(HttpClient httpClient) : IProjectRoleGridClient
{
    public async Task<ProjectRoleGridViewResult> GetRoleGrid(ProjectRolesListIdentification id)
    {
        return await httpClient.GetFromJsonAsync<ProjectRoleGridViewResult>(
            $"webapi/project-role-grid/get?projectId={id.ProjectId.Value}&projectRolesListId={id.ProjectRolesListId}")
            ?? throw new Exception("Couldn't get result from server");
    }
}
