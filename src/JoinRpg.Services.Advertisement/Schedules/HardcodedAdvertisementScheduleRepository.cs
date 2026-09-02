namespace JoinRpg.Services.Advertisement.Schedules;

// TODO: заменить на БД-backed реализацию, когда появится таблица AdvertisementSchedules (ADR010 §3)
internal class HardcodedAdvertisementScheduleRepository(IAdvertisementChannelRepository channelRepository) : IAdvertisementScheduleRepository
{
    private static readonly IReadOnlySet<DayOfWeek> EveryDay = Enum.GetValues<DayOfWeek>().ToHashSet();
    private static readonly IReadOnlySet<DayOfWeek> Wednesdays = new HashSet<DayOfWeek> { DayOfWeek.Wednesday };

    public async Task<IReadOnlyCollection<AdvertisementScheduleInfo>> GetActiveSchedules() =>
        [.. (await GetAllSchedules()).Where(s => s.IsEffectivelyActive)];

    public async Task<IReadOnlyCollection<AdvertisementScheduleInfo>> GetAllSchedules()
    {
        var schedules = new List<AdvertisementScheduleInfo>();

        if (await channelRepository.GetChannel(HardcodedAdvertisementChannelRepository.HotRoleChannelId) is { } hotRoleChannel)
        {
            schedules.Add(new(new AdvertisementScheduleIdentification(1), hotRoleChannel, AdvertisementMethod.SingleHotRole, EveryDay));
        }

        if (await channelRepository.GetChannel(HardcodedAdvertisementChannelRepository.ZovemChannelId) is { } zovemChannel)
        {
            schedules.Add(new(new AdvertisementScheduleIdentification(2), zovemChannel, AdvertisementMethod.SingleHotRole, Wednesdays));
        }

        return schedules;
    }
}
