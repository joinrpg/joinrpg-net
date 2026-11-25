namespace JoinRpg.Data.Interfaces;

public interface IPostingChannelRepository
{
    Task<PostingChannelInfo?> GetChannel(PostingChannelIdentification channelId);

    Task<IReadOnlyCollection<PostingChannelInfo>> GetAllChannels();
}
