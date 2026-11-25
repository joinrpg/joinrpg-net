namespace JoinRpg.Data.Interfaces;

public interface IAdvertisementChannelRepository
{
    Task<PostingChannelInfo?> GetChannel(PostingChannelIdentification channelId);

    Task<IReadOnlyCollection<PostingChannelInfo>> GetAllChannels();
}
