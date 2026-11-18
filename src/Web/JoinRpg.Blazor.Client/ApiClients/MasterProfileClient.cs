using JoinRpg.Web.ProjectMasterTools.Acl;

namespace JoinRpg.Blazor.Client.ApiClients;

public class MasterProfileClient(HttpClient httpClient) : IMasterProfileClient
{
    public async Task<MasterProfileViewModel> GetProfile(ProjectIdentification projectId, UserIdentification userId)
        => await (await httpClient.GetAsync($"webapi/{projectId.Value}/master-profile/GetProfile?userId={userId.Value}"))
            .ReadFromJsonOrThrowProblem<MasterProfileViewModel>();

    public async Task SaveProfile(MasterProfileViewModel model)
    {
        var response = await httpClient.PostAsJsonAsync($"/webapi/{model.ProjectId.Value}/master-profile/SaveProfile", model);
        await response.EnsureSuccessOrThrowProblem();
    }
}
