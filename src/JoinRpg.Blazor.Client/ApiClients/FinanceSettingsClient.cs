using JoinRpg.DomainTypes.ProjectMetadata.Payments;
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

    async Task<FinanceFeeSettingsViewModel> IFinanceSettingsClient.GetFeeSettings(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/{projectId.Value}/finance-settings/GetFeeSettings"))
            .ReadFromJsonOrThrowProblem<FinanceFeeSettingsViewModel>();

    async Task IFinanceSettingsClient.CreateFeeSetting(ProjectIdentification projectId, CreateFeeSettingViewModel model)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsJsonAsync($"webapi/{projectId.Value}/finance-settings/CreateFeeSetting", model);
        await response.EnsureSuccessOrThrowProblem();
    }

    async Task IFinanceSettingsClient.DeleteFeeSetting(ProjectIdentification projectId, int projectFeeSettingId)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync(
            $"webapi/{projectId.Value}/finance-settings/DeleteFeeSetting?projectFeeSettingId={projectFeeSettingId}", content: null);
        await response.EnsureSuccessOrThrowProblem();
    }

    async Task<FinancePaymentTypesViewModel> IFinanceSettingsClient.GetPaymentTypes(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/{projectId.Value}/finance-settings/GetPaymentTypes"))
            .ReadFromJsonOrThrowProblem<FinancePaymentTypesViewModel>();

    async Task IFinanceSettingsClient.CreatePaymentType(ProjectIdentification projectId, NewPaymentTypeViewModel model)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsJsonAsync($"webapi/{projectId.Value}/finance-settings/CreatePaymentType", model);
        await response.EnsureSuccessOrThrowProblem();
    }

    async Task IFinanceSettingsClient.TogglePaymentType(PaymentTypeIdentification paymentTypeId)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync(
            $"webapi/{paymentTypeId.ProjectId.Value}/finance-settings/TogglePaymentType?paymentTypeId={paymentTypeId.PaymentTypeId}", content: null);
        await response.EnsureSuccessOrThrowProblem();
    }
}
