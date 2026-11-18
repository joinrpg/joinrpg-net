using JoinRpg.Web.ProjectMasterTools.Acl;

namespace JoinRpg.Blazor.Client.ApiClients;

public class MasterAccessClient(HttpClient httpClient) : IMasterAccessClient
{
    public async Task<AddMasterViewModel> GetAddMaster(ProjectIdentification projectId, UserIdentification userId)
        => await (await httpClient.GetAsync($"webapi/{projectId.Value}/master-access/GetAddMaster?userId={userId.Value}"))
            .ReadFromJsonOrThrowProblem<AddMasterViewModel>();

    public async Task AddMaster(AddMasterViewModel model)
    {
        var response = await httpClient.PostAsJsonAsync($"/webapi/{model.ProjectId.Value}/master-access/AddMaster", model);
        await response.EnsureSuccessOrThrowProblem();
    }

    public async Task<MasterPermissionsViewModel> GetPermissions(ProjectIdentification projectId, UserIdentification userId)
        => await (await httpClient.GetAsync($"webapi/{projectId.Value}/master-access/GetPermissions?userId={userId.Value}"))
            .ReadFromJsonOrThrowProblem<MasterPermissionsViewModel>();

    public async Task SavePermissions(MasterPermissionsViewModel model)
    {
        var response = await httpClient.PostAsJsonAsync($"/webapi/{model.ProjectId.Value}/master-access/SavePermissions", model);
        await response.EnsureSuccessOrThrowProblem();
    }
}
