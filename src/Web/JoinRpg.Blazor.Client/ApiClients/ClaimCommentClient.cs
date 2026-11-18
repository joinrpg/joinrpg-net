using JoinRpg.Web.Claims.Comments;

namespace JoinRpg.Blazor.Client.ApiClients;

public class ClaimCommentClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IClaimCommentClient
{
    public async Task AddComment(AddClaimCommentRequest request)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsJsonAsync("webapi/claim-comment/AddComment", request);

        // Причина отказа (нет доступа к заявке, пустой текст) приходит текстом, чтобы форма показала её автору
        await response.EnsureSuccessOrThrowProblem();
    }
}
