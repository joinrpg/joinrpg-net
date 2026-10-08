using JoinRpg.Web.ProjectMasterTools.Acl;

namespace JoinRpg.Blazor.Client.ApiClients;

public class MasterAccessClient(HttpClient httpClient) : IMasterAccessClient
{
    public async Task<AddMasterViewModel> GetAddMaster(ProjectIdentification projectId, UserIdentification userId)
        => await httpClient.GetFromJsonAsync<AddMasterViewModel>($"webapi/{projectId.Value}/master-access/GetAddMaster?userId={userId.Value}")
            ?? throw new Exception("Couldn't get result from server");

    public async Task AddMaster(AddMasterViewModel model)
    {
        var response = await httpClient.PostAsJsonAsync($"/webapi/{model.ProjectId.Value}/master-access/AddMaster", model);
        _ = response.EnsureSuccessStatusCode();
    }

    public async Task<MasterPermissionsViewModel> GetPermissions(ProjectIdentification projectId, UserIdentification userId)
        => await httpClient.GetFromJsonAsync<MasterPermissionsViewModel>($"webapi/{projectId.Value}/master-access/GetPermissions?userId={userId.Value}")
            ?? throw new Exception("Couldn't get result from server");

    public async Task SavePermissions(MasterPermissionsViewModel model)
    {
        var response = await httpClient.PostAsJsonAsync($"/webapi/{model.ProjectId.Value}/master-access/SavePermissions", model);
        _ = response.EnsureSuccessStatusCode();
    }
}
