using JoinRpg.DomainTypes.Posting;

namespace JoinRpg.Dal.Impl.Repositories;

internal class AdvertisementChannelRepository(MyDbContext ctx) : IAdvertisementChannelRepository
{
    public async Task<PostingChannelInfo?> GetChannel(PostingChannelIdentification channelId)
    {
        var entity = await ctx.AdvertisementChannelsSet
            .FirstOrDefaultAsync(c => c.AdvertisementChannelId == channelId.Value);
        return entity is null ? null : AdvertisementEntityMapper.ToInfo(entity);
    }

    public async Task<IReadOnlyCollection<PostingChannelInfo>> GetAllChannels() =>
        [.. (await ctx.AdvertisementChannelsSet.ToListAsync()).Select(AdvertisementEntityMapper.ToInfo)];
}
