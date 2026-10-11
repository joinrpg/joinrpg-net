using JoinRpg.Web.ProjectMasterTools.ClaimRestore;

namespace JoinRpg.Blazor.Client.ApiClients;

public class ClaimRestoreClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IClaimRestoreClient
{
    public async Task<ClaimRestorePlanViewModel> GetPlan(ProjectIdentification projectId)
        => await httpClient.GetFromJsonAsync<ClaimRestorePlanViewModel>(
            $"webapi/claim-restore/GetPlan?projectId={projectId.Value}")
            ?? throw new Exception("Не удалось получить план с сервера");

    public async Task<ClaimRestoreApplyResult> Apply(ClaimRestoreApplyRequest request)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsJsonAsync(
            $"webapi/claim-restore/Apply?projectId={request.ClaimId.ProjectId.Value}",
            request);

        if (!response.IsSuccessStatusCode)
        {
            // Причину отказа сервер отдаёт текстом — её показываем мастеру в строке плана.
            var errorMessage = await response.Content.ReadAsStringAsync();
            throw new Exception(string.IsNullOrWhiteSpace(errorMessage) ? "Не удалось применить строку" : errorMessage.Trim('"'));
        }

        return await response.Content.ReadFromJsonAsync<ClaimRestoreApplyResult>()
            ?? throw new Exception("Не удалось получить результат с сервера");
    }
}
