using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class ProjectRoleGridClientImpl(HttpClient httpClient) : IProjectRoleGridClient
{
    public async Task<ProjectRoleGridViewResult> GetRoleGrid(ProjectRolesListIdentification id)
        => await (await httpClient.GetAsync(
            $"webapi/project-role-grid/get?projectId={id.ProjectId.Value}&projectRolesListId={id.ProjectRolesListId}"))
            .ReadFromJsonOrThrowProblem<ProjectRoleGridViewResult>();

    public async Task<ProjectRoleGridViewResult> GetClassicRoleGrid(ProjectIdentification projectId, CharacterGroupIdentification? groupId, bool hotOnly = false)
    {
        var url = $"webapi/project-role-grid/getclassic?projectId={projectId.Value}"
            + (groupId is { } g ? $"&characterGroupId={g.CharacterGroupId}" : "")
            + (hotOnly ? "&hotOnly=true" : "");
        return await (await httpClient.GetAsync(url)).ReadFromJsonOrThrowProblem<ProjectRoleGridViewResult>();
    }
}
