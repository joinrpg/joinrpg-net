using System.Net.Http.Headers;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;

namespace JoinRpg.Blazor.Client.ApiClients;

public class ClaimRestoreClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IClaimRestoreClient
{
    public async Task<ClaimRestorePlanViewModel> GetPlan(ClaimRestorePlanRequest request)
    {
        var response = await Post($"GetPlan?projectId={request.ProjectId.Value}", JsonContent.Create(request), "Не удалось получить план с сервера");
        return await response.Content.ReadFromJsonAsync<ClaimRestorePlanViewModel>()
            ?? throw new Exception("Не удалось получить план с сервера");
    }

    public async Task<MasterExportUploadResult> ParseExport(ProjectIdentification projectId, string fileName, Stream content)
    {
        using var form = new MultipartFormDataContent();
        var file = new StreamContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", fileName);
        var response = await Post($"ParseExport?projectId={projectId.Value}", form, "Не удалось загрузить файл", "Файл слишком большой");
        return await response.Content.ReadFromJsonAsync<MasterExportUploadResult>()
            ?? throw new Exception("Не удалось получить результат с сервера");
    }

    public async Task<ClaimRestoreApplyResult> Apply(ClaimRestoreApplyRequest request)
    {
        var response = await Post($"Apply?projectId={request.ClaimId.ProjectId.Value}", JsonContent.Create(request));
        return await response.Content.ReadFromJsonAsync<ClaimRestoreApplyResult>()
            ?? throw new Exception("Не удалось получить результат с сервера");
    }

    public async Task ApplyGroup(GroupRestoreApplyRequest request)
        => _ = await Post($"ApplyGroup?projectId={request.GroupId.ProjectId.Value}", JsonContent.Create(request));

    public async Task ApplyCharacter(CharacterRestoreApplyRequest request)
        => _ = await Post($"ApplyCharacter?projectId={request.CharacterId.ProjectId.Value}", JsonContent.Create(request));

    public async Task ApplyClaimFields(ClaimFieldsRestoreApplyRequest request)
        => _ = await Post($"ApplyClaimFields?projectId={request.ClaimId.ProjectId.Value}", JsonContent.Create(request));

    private async Task<HttpResponseMessage> Post(
        string action,
        HttpContent content,
        string defaultError = "Не удалось применить строку",
        string tooLargeError = "Сервер не принял запрос: слишком большой объём выгрузок")
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync($"webapi/claim-restore/{action}", content);

        if (response.StatusCode == System.Net.HttpStatusCode.RequestEntityTooLarge)
        {
            throw new Exception(tooLargeError);
        }
        if (!response.IsSuccessStatusCode)
        {
            // Причину отказа сервер отдаёт текстом — её показываем мастеру.
            var errorMessage = await response.Content.ReadAsStringAsync();
            throw new Exception(string.IsNullOrWhiteSpace(errorMessage) ? defaultError : errorMessage.Trim('"'));
        }
        return response;
    }
}
