using JoinRpg.Web.AdminTools.Advertisement;

namespace JoinRpg.Blazor.Client.ApiClients;

public class PostingChannelHttpClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IPostingChannelClient
{
    public async Task<AvailableChatsResponse> GetAvailableChats()
        => await (await httpClient.GetAsync("webapi/PostingChannel/GetAvailableChats"))
            .ReadFromJsonOrThrowProblem<AvailableChatsResponse>();

    public async Task CreateChannel(CreateChannelRequest request)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync(
            $"webapi/PostingChannel/CreateChannel?chatId={request.ChatId}&name={Uri.EscapeDataString(request.Name)}",
            content: null);

        // Отказ (чат занят, бот не в чате и т.п.) диалог показывает как есть
        await response.EnsureSuccessOrThrowProblem();
    }
}
