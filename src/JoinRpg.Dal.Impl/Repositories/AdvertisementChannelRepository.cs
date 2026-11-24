using JoinRpg.DomainTypes.Advertisement;

namespace JoinRpg.Dal.Impl.Repositories;

internal class AdvertisementChannelRepository(MyDbContext ctx) : IAdvertisementChannelRepository
{
    public async Task<AdvertisementChannelInfo?> GetChannel(AdvertisementChannelIdentification channelId)
    {
        var entity = await ctx.AdvertisementChannelsSet
            .FirstOrDefaultAsync(c => c.AdvertisementChannelId == channelId.Value);
        return entity is null ? null : AdvertisementEntityMapper.ToInfo(entity);
    }

    public async Task<IReadOnlyCollection<AdvertisementChannelInfo>> GetAllChannels() =>
        [.. (await ctx.AdvertisementChannelsSet.ToListAsync()).Select(AdvertisementEntityMapper.ToInfo)];
}
