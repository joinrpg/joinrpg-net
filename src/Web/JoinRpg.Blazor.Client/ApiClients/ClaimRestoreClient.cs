using System.Net;
using System.Net.Http.Headers;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;

namespace JoinRpg.Blazor.Client.ApiClients;

public class ClaimRestoreClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IClaimRestoreClient
{
    public async Task<ClaimRestorePlanViewModel> GetPlan(ClaimRestorePlanRequest request)
        => await (await Post($"GetPlan?projectId={request.ProjectId.Value}", JsonContent.Create(request)))
            .ReadFromJsonOrThrowProblem<ClaimRestorePlanViewModel>();

    public async Task<MasterExportUploadResult> ParseExport(ProjectIdentification projectId, string fileName, Stream content)
    {
        using var form = new MultipartFormDataContent();
        var file = new StreamContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", fileName);
        return await (await Post($"ParseExport?projectId={projectId.Value}", form, "Файл слишком большой"))
            .ReadFromJsonOrThrowProblem<MasterExportUploadResult>();
    }

    public async Task<ClaimRestoreApplyResult> Apply(ClaimRestoreApplyRequest request)
        => await (await Post($"Apply?projectId={request.ClaimId.ProjectId.Value}", JsonContent.Create(request)))
            .ReadFromJsonOrThrowProblem<ClaimRestoreApplyResult>();

    public async Task ApplyGroup(GroupRestoreApplyRequest request)
        => await (await Post($"ApplyGroup?projectId={request.GroupId.ProjectId.Value}", JsonContent.Create(request)))
            .EnsureSuccessOrThrowProblem();

    public async Task ApplyCharacter(CharacterRestoreApplyRequest request)
        => await (await Post($"ApplyCharacter?projectId={request.CharacterId.ProjectId.Value}", JsonContent.Create(request)))
            .EnsureSuccessOrThrowProblem();

    public async Task ApplyClaimFields(ClaimFieldsRestoreApplyRequest request)
        => await (await Post($"ApplyClaimFields?projectId={request.ClaimId.ProjectId.Value}", JsonContent.Create(request)))
            .EnsureSuccessOrThrowProblem();

    private async Task<HttpResponseMessage> Post(
        string action,
        HttpContent content,
        string tooLargeError = "Сервер не принял запрос: слишком большой объём выгрузок")
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync($"webapi/claim-restore/{action}", content);

        // 413 отдаёт не контроллер, а лимит размера запроса — тела с причиной там нет
        if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge)
        {
            throw new ServerProblemException(tooLargeError);
        }
        return response;
    }
}
