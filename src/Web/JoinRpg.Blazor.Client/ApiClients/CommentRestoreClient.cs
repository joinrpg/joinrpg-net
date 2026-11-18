using JoinRpg.Web.ProjectMasterTools.ClaimRestore;

namespace JoinRpg.Blazor.Client.ApiClients;

public class CommentRestoreClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : ICommentRestoreClient
{
    public async Task<CommentRestorePlanViewModel> GetPlan(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/comment-restore/GetPlan?projectId={projectId.Value}"))
            .ReadFromJsonOrThrowProblem<CommentRestorePlanViewModel>();

    public async Task<CommentRestoreApplyResult> Apply(ClaimIdentification claimId)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync(
            $"webapi/comment-restore/Apply?projectId={claimId.ProjectId.Value}",
            JsonContent.Create(claimId));
        return await response.ReadFromJsonOrThrowProblem<CommentRestoreApplyResult>();
    }
}
