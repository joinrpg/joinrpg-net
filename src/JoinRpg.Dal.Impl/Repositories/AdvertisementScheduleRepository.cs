using JoinRpg.DomainTypes.Advertisement;

namespace JoinRpg.Dal.Impl.Repositories;

internal class AdvertisementScheduleRepository(MyDbContext ctx) : IAdvertisementScheduleRepository
{
    private IQueryable<AdvertisementScheduleEntity> Query =>
        ctx.AdvertisementSchedulesSet.Include(s => s.Channel);

    public async Task<IReadOnlyCollection<AdvertisementScheduleInfo>> GetActiveSchedules() =>
        [.. (await Query
            .Where(s => s.IsActive && s.Channel.IsActive)
            .ToListAsync())
            .Select(AdvertisementEntityMapper.ToInfo)];

    public async Task<IReadOnlyCollection<AdvertisementScheduleInfo>> GetAllSchedules() =>
        [.. (await Query.ToListAsync()).Select(AdvertisementEntityMapper.ToInfo)];
}
