namespace JoinRpg.Services.Advertisement.Channels;

// TODO: заменить на БД-backed реализацию, когда появится таблица AdvertisementChannels (ADR010 §3)
internal class HardcodedAdvertisementChannelRepository : IAdvertisementChannelRepository
{
    public static readonly AdvertisementChannelIdentification HotRoleChannelId = new(1);
    public static readonly AdvertisementChannelIdentification ZovemChannelId = new(2);

    private static readonly AdvertisementChannelInfo HotRoleChannel = new(
        HotRoleChannelId,
        Name: "test_zovem_na_igru",
        BoundProjectId: null,
        new TelegramChannelSettings(new TelegramChatId(-1004315256401)));

    private static readonly AdvertisementChannelInfo ZovemChannel = new(
        ZovemChannelId,
        Name: "t.me/zovem_joinrpg",
        BoundProjectId: null,
        new TelegramChannelSettings(new TelegramChatId(-1002544815071)));

    private static readonly IReadOnlyList<AdvertisementChannelInfo> AllChannels = [HotRoleChannel, ZovemChannel];

    public Task<AdvertisementChannelInfo?> GetChannel(AdvertisementChannelIdentification channelId) =>
        Task.FromResult(AllChannels.FirstOrDefault(c => c.ChannelId == channelId));

    public Task<IReadOnlyCollection<AdvertisementChannelInfo>> GetAllChannels() =>
        Task.FromResult<IReadOnlyCollection<AdvertisementChannelInfo>>(AllChannels);
}
