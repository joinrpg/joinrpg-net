namespace JoinRpg.Data.Interfaces;

public interface IAdvertisementChannelRepository
{
    Task<AdvertisementChannelInfo?> GetChannel(AdvertisementChannelIdentification channelId);
}
