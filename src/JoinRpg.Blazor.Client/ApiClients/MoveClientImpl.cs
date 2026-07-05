using JoinRpg.Web.ProjectCommon.ElementMoving;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class MoveClientImpl(
    HttpClient httpClient,
    CsrfTokenProvider csrfTokenProvider,
    ILogger<MoveClientImpl> logger) : IMoveClient
{
    private record WireMoveRequest(string SelfId, string ParentId, string? MoveAfterId);

    public async Task<string[]> MoveAfterAsync(MoveRequest request)
    {
        try
        {
            var wire = new WireMoveRequest(
                request.SelfId.ToString()!,
                request.ParentId.ToString()!,
                request.MoveAfterId?.ToString());
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsJsonAsync($"webapi/move/moveafter?ProjectId={request.SelfId.ProjectId.Value}", wire);
            return await response
                .EnsureSuccessStatusCode()
                .Content
                .ReadFromJsonAsync<string[]>()
                ?? throw new Exception("Empty response from move endpoint");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during MoveAfter");
            throw;
        }
    }
}
