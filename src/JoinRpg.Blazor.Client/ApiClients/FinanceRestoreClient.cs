using JoinRpg.Web.ProjectMasterTools.ClaimRestore;

namespace JoinRpg.Blazor.Client.ApiClients;

public class FinanceRestoreClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IFinanceRestoreClient
{
    public async Task<FinanceRestorePlanViewModel> GetPlan(ProjectIdentification projectId)
    {
        var response = await httpClient.GetAsync($"webapi/finance-restore/GetPlan?projectId={projectId.Value}");
        await EnsureSuccess(response, "Не удалось получить план с сервера");
        return await response.Content.ReadFromJsonAsync<FinanceRestorePlanViewModel>()
            ?? throw new Exception("Не удалось получить план с сервера");
    }

    public async Task<FinanceRestoreApplyResult> Apply(FinanceRestoreApplyRequest request)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync(
            $"webapi/finance-restore/Apply?projectId={request.ClaimId.ProjectId.Value}",
            JsonContent.Create(request));
        await EnsureSuccess(response, "Не удалось восстановить оплаты заявки");
        return await response.Content.ReadFromJsonAsync<FinanceRestoreApplyResult>()
            ?? throw new Exception("Не удалось получить результат с сервера");
    }

    public async Task CreateOwnCash(ProjectIdentification projectId)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync($"webapi/finance-restore/CreateOwnCash?projectId={projectId.Value}", content: null);
        await EnsureSuccess(response, "Не удалось создать тип оплаты «Наличные»");
    }

    private static async Task EnsureSuccess(HttpResponseMessage response, string defaultError)
    {
        if (!response.IsSuccessStatusCode)
        {
            // Причину отказа сервер отдаёт текстом — её показываем мастеру.
            var errorMessage = await response.Content.ReadAsStringAsync();
            throw new Exception(string.IsNullOrWhiteSpace(errorMessage) ? defaultError : errorMessage.Trim('"'));
        }
    }
}
