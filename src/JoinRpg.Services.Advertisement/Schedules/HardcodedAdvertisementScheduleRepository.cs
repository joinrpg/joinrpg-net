namespace JoinRpg.Services.Advertisement.Schedules;

// TODO: заменить на БД-backed реализацию, когда появится таблица AdvertisementSchedules (ADR010 §3)
internal class HardcodedAdvertisementScheduleRepository(IAdvertisementChannelRepository channelRepository) : IAdvertisementScheduleRepository
{
    private static readonly IReadOnlySet<DayOfWeek> EveryDay = Enum.GetValues<DayOfWeek>().ToHashSet();

    public async Task<IReadOnlyCollection<AdvertisementScheduleInfo>> GetActiveSchedules()
    {
        var channel = await channelRepository.GetChannel(HardcodedAdvertisementChannelRepository.HotRoleChannelId);
        if (channel is null)
        {
            return [];
        }

        return [new(new AdvertisementScheduleIdentification(1), channel, AdvertisementMethod.SingleHotRole, EveryDay)];
    }
}
