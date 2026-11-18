namespace JoinRpg.Blazor.Client.ApiClients;

// Ошибки пишет в лог JoinMoveControl, отказ сервера он же показывает
internal class MoveClientImpl(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IMoveClient
{
    private record WireMoveRequest(string SelfId, string ParentId, string? MoveAfterId);

    public async Task<string[]> MoveAfterAsync(string selfId, string parentId, string? moveAfterId)
    {
        if (!ProjectEntityIdParser.TryParseId(selfId, out var self))
        {
            throw new ArgumentException($"Cannot parse selfId: '{selfId}'");
        }

        var wire = new WireMoveRequest(selfId, parentId, moveAfterId);
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsJsonAsync($"webapi/move/moveafter?ProjectId={self.ProjectId.Value}", wire);
        return await response.ReadFromJsonOrThrowProblem<string[]>();
    }
}
