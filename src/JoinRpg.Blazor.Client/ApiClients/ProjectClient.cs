using JoinRpg.Web.AdminTools;
using JoinRpg.Web.Games.Projects;
using JoinRpg.Web.ProjectCommon.Projects;
using JoinRpg.Web.ProjectMasterTools.Settings;

namespace JoinRpg.Blazor.Client.ApiClients;

public class ProjectListClient(HttpClient httpClient) : IProjectListClient, IProjectListForAdminClient
{
    public async Task<List<ProjectLinkViewModel>> GetProjects(ProjectSelectionCriteria projectSelectionCriteria)
    {
        return await (await httpClient.GetAsync($"webapi/projects/GetProjects?criteria={projectSelectionCriteria}"))
            .ReadFromJsonOrThrowProblem<List<ProjectLinkViewModel>>();
    }

    public async Task<List<ProjectAdminListItemViewModel>> GetProjectsForAdmin(ProjectSelectionCriteria projectSelectionCriteria)
    {
        return await (await httpClient.GetAsync($"webapi/projects/GetProjectsForAdmin?criteria={projectSelectionCriteria}"))
            .ReadFromJsonOrThrowProblem<List<ProjectAdminListItemViewModel>>();
    }
}

public class ProjectCreateClient(HttpClient httpClient) : IProjectCreateClient
{
    public async Task<ProjectCreateResultViewModel> CreateProject(ProjectCreateViewModel model)
    {
        var response = await httpClient.PostAsJsonAsync("/webapi/project-create/create", model);
        return await response.ReadFromJsonOrThrowProblem<ProjectCreateResultViewModel>();
    }

    public async Task<bool> IsProductionEnvironment()
        => await (await httpClient.GetAsync("/webapi/project-create/IsProductionEnvironment")).ReadFromJsonOrThrowProblem<bool>();
}

public class ProjectSettingsClient(HttpClient httpClient) : IProjectSettingsClient
{
    async Task<ProjectMainSettingsViewModel> IProjectSettingsClient.GetMainSettings(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/{projectId.Value}/project/GetMainSettings")).ReadFromJsonOrThrowProblem<ProjectMainSettingsViewModel>();
    async Task IProjectSettingsClient.SaveMainSettings(ProjectMainSettingsViewModel model)
    {
        var response = await httpClient.PostAsJsonAsync($"/webapi/{model.ProjectId.Value}/project/SaveMainSettings", model);
        await response.EnsureSuccessOrThrowProblem();
    }

    async Task<ProjectPublishSettingsViewModel> IProjectSettingsClient.GetPublishSettings(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/{projectId.Value}/project/GetPublishSettings")).ReadFromJsonOrThrowProblem<ProjectPublishSettingsViewModel>();
    async Task IProjectSettingsClient.SavePublishSettings(ProjectPublishSettingsViewModel model)
    {
        var response = await httpClient.PostAsJsonAsync($"/webapi/{model.ProjectId.Value}/project/SavePublishSettings", model);
        await response.EnsureSuccessOrThrowProblem();
    }

    async Task<ProjectContactsSettingsViewModel> IProjectSettingsClient.GetContactSettings(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/{projectId.Value}/project/GetContactSettings")).ReadFromJsonOrThrowProblem<ProjectContactsSettingsViewModel>();
    async Task IProjectSettingsClient.SaveContactSettings(ProjectContactsSettingsViewModel model)
    {
        var response = await httpClient.PostAsJsonAsync($"/webapi/{model.ProjectId.Value}/project/SaveContactSettings", model);
        await response.EnsureSuccessOrThrowProblem();
    }

    async Task IProjectSettingsClient.SaveClaimSettings(ProjectClaimSettingsViewModel model)
    {
        var response = await httpClient.PostAsJsonAsync($"/webapi/{model.ProjectId.Value}/project/SaveClaimSettings", model);
        await response.EnsureSuccessOrThrowProblem();
    }
    async Task<ProjectClaimSettingsViewModel> IProjectSettingsClient.GetClaimSettings(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/{projectId.Value}/project/GetClaimSettings")).ReadFromJsonOrThrowProblem<ProjectClaimSettingsViewModel>();

    async Task IProjectSettingsClient.SaveTimeZoneSettings(ProjectTimeZoneSettingsViewModel model)
    {
        var response = await httpClient.PostAsJsonAsync($"/webapi/{model.ProjectId.Value}/project/SaveTimeZoneSettings", model);
        await response.EnsureSuccessOrThrowProblem();
    }
    async Task<ProjectTimeZoneSettingsViewModel> IProjectSettingsClient.GetTimeZoneSettings(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/{projectId.Value}/project/GetTimeZoneSettings")).ReadFromJsonOrThrowProblem<ProjectTimeZoneSettingsViewModel>();
}

public class ProjectInfoClient(HttpClient httpClient) : IProjectInfoClient
{
    async Task<ProjectInfoViewModel> IProjectInfoClient.GetProjectInfo(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/{projectId.Value}/project-info/GetProjectInfo")).ReadFromJsonOrThrowProblem<ProjectInfoViewModel>();
}
