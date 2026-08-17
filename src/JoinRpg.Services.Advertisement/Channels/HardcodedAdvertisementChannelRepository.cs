namespace JoinRpg.Services.Advertisement.Channels;

// TODO: заменить на БД-backed реализацию, когда появится таблица AdvertisementChannels (ADR010 §3)
internal class HardcodedAdvertisementChannelRepository : IAdvertisementChannelRepository
{
    public static readonly AdvertisementChannelIdentification HotRoleChannelId = new(1);

    private static readonly AdvertisementChannelInfo HotRoleChannel = new(
        HotRoleChannelId,
        BoundProjectId: null,
        new TelegramChannelSettings(new TelegramId(-1004315256401, null)));

    public Task<AdvertisementChannelInfo?> GetChannel(AdvertisementChannelIdentification channelId) =>
        Task.FromResult(channelId == HotRoleChannelId ? HotRoleChannel : null);
}
