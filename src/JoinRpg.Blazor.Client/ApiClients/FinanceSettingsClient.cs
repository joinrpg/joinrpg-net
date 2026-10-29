using JoinRpg.Web.ProjectMasterTools.Finance;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class FinanceSettingsClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IFinanceSettingsClient
{
    async Task<FinanceGeneralSettingsViewModel> IFinanceSettingsClient.GetGeneralSettings(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/{projectId.Value}/finance-settings/GetGeneralSettings"))
            .ReadFromJsonOrThrowProblem<FinanceGeneralSettingsViewModel>();

    async Task IFinanceSettingsClient.SaveGeneralSettings(FinanceGeneralSettingsViewModel model)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsJsonAsync($"webapi/{model.ProjectId.Value}/finance-settings/SaveGeneralSettings", model);
        await response.EnsureSuccessOrThrowProblem();
    }
}
