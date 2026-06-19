using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class ProjectRoleGridClientImpl(HttpClient httpClient) : IProjectRoleGridClient
{
    public async Task<ProjectRoleGridViewModel> GetRoleGrid(ProjectRolesListIdentification id)
    {
        return await httpClient.GetFromJsonAsync<ProjectRoleGridViewModel>(
            $"webapi/project-role-grid/get?projectId={id.ProjectId.Value}&projectRolesListId={id.ProjectRolesListId}")
            ?? throw new Exception("Couldn't get result from server");
    }
}
