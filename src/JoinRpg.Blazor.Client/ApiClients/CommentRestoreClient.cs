using JoinRpg.Web.ProjectMasterTools.ClaimRestore;

namespace JoinRpg.Blazor.Client.ApiClients;

public class CommentRestoreClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : ICommentRestoreClient
{
    public async Task<CommentRestorePlanViewModel> GetPlan(ProjectIdentification projectId)
    {
        var response = await httpClient.GetAsync($"webapi/comment-restore/GetPlan?projectId={projectId.Value}");
        await EnsureSuccess(response, "Не удалось получить план с сервера");
        return await response.Content.ReadFromJsonAsync<CommentRestorePlanViewModel>()
            ?? throw new Exception("Не удалось получить план с сервера");
    }

    public async Task<CommentRestoreApplyResult> Apply(ClaimIdentification claimId)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync(
            $"webapi/comment-restore/Apply?projectId={claimId.ProjectId.Value}",
            JsonContent.Create(claimId));
        await EnsureSuccess(response, "Не удалось восстановить комментарии заявки");
        return await response.Content.ReadFromJsonAsync<CommentRestoreApplyResult>()
            ?? throw new Exception("Не удалось получить результат с сервера");
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
