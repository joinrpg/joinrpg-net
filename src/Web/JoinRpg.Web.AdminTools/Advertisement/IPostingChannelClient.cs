namespace JoinRpg.Web.AdminTools.Advertisement;

public interface IPostingChannelClient
{
    Task<AvailableChatsResponse> GetAvailableChats();
    Task CreateChannel(CreateChannelRequest request);
}

public record AvailableChatsResponse(bool Enabled, string? BotName, IReadOnlyCollection<AvailableChatDto> Chats);

public record AvailableChatDto(long ChatId, string Title);

public record CreateChannelRequest(long ChatId, string Name);
