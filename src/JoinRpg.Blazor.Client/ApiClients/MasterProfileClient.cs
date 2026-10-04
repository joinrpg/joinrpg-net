using JoinRpg.Web.ProjectMasterTools.Acl;

namespace JoinRpg.Blazor.Client.ApiClients;

public class MasterProfileClient(HttpClient httpClient) : IMasterProfileClient
{
    public async Task<MasterProfileViewModel> GetProfile(ProjectIdentification projectId, UserIdentification userId)
        => await httpClient.GetFromJsonAsync<MasterProfileViewModel>($"webapi/{projectId.Value}/master-profile/GetProfile?userId={userId.Value}")
            ?? throw new Exception("Couldn't get result from server");

    public async Task SaveProfile(MasterProfileViewModel model)
    {
        var response = await httpClient.PostAsJsonAsync($"/webapi/{model.ProjectId.Value}/master-profile/SaveProfile", model);
        _ = response.EnsureSuccessStatusCode();
    }
}
