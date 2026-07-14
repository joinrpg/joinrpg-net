using JoinRpg.DomainTypes;
using JoinRpg.WebComponents;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class MoveClientImpl(
    HttpClient httpClient,
    CsrfTokenProvider csrfTokenProvider,
    ILogger<MoveClientImpl> logger) : IMoveClient
{
    private record WireMoveRequest(string SelfId, string ParentId, string? MoveAfterId);

    public async Task<string[]> MoveAfterAsync(string selfId, string parentId, string? moveAfterId)
    {
        try
        {
            if (!ProjectEntityIdParser.TryParseId(selfId, out var self))
                throw new ArgumentException($"Cannot parse selfId: '{selfId}'");

            var wire = new WireMoveRequest(selfId, parentId, moveAfterId);
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsJsonAsync($"webapi/move/moveafter?ProjectId={self.ProjectId.Value}", wire);
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
