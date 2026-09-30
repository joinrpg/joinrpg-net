using JoinRpg.Services.Advertisement.Channels;
using JoinRpg.Services.Advertisement.Schedules;

namespace JoinRpg.Services.Advertisement.Test;

public class HardcodedAdvertisementScheduleRepositoryTests
{
    [Fact]
    public async Task GetActiveSchedules_ReturnsScheduleForZovemChannelOnWednesdays()
    {
        var repository = new HardcodedAdvertisementScheduleRepository(new HardcodedAdvertisementChannelRepository());

        var schedules = await repository.GetActiveSchedules();

        var schedule = schedules.Single(s => s.Method == AdvertisementMethod.SingleHotRole);
        schedule.Channel.ChannelId.ShouldBe(HardcodedAdvertisementChannelRepository.ZovemChannelId);
        schedule.Days.ShouldBe([DayOfWeek.Wednesday], ignoreOrder: true);
    }

    [Fact]
    public async Task GetActiveSchedules_ReturnsNewlyOpenedProjectsDigestForZovemChannelOnMondays()
    {
        var repository = new HardcodedAdvertisementScheduleRepository(new HardcodedAdvertisementChannelRepository());

        var schedules = await repository.GetActiveSchedules();

        var schedule = schedules.Single(s => s.Method == AdvertisementMethod.NewlyOpenedProjectsDigest);
        schedule.Channel.ChannelId.ShouldBe(HardcodedAdvertisementChannelRepository.ZovemChannelId);
        schedule.Days.ShouldBe([DayOfWeek.Monday], ignoreOrder: true);
    }

    [Fact]
    public async Task GetActiveSchedules_DoesNotReturnAnythingForTestChannel()
    {
        var repository = new HardcodedAdvertisementScheduleRepository(new HardcodedAdvertisementChannelRepository());

        var schedules = await repository.GetActiveSchedules();

        schedules.ShouldAllBe(s => s.Channel.ChannelId != HardcodedAdvertisementChannelRepository.TestChannelId);
    }

    [Fact]
    public async Task GetAllSchedules_ReturnsAllChannelsRegardlessOfActivity()
    {
        var repository = new HardcodedAdvertisementScheduleRepository(new HardcodedAdvertisementChannelRepository());

        var schedules = await repository.GetAllSchedules();

        schedules.Count.ShouldBe(4);
    }

    [Fact]
    public async Task GetAllSchedules_KeepsStableScheduleIds()
    {
        // Идентификаторы расписаний попадают в лог отправок, менять их у существующих расписаний нельзя.
        var repository = new HardcodedAdvertisementScheduleRepository(new HardcodedAdvertisementChannelRepository());

        var schedules = await repository.GetAllSchedules();

        schedules.Select(s => (s.ScheduleId.Value, s.Channel.ChannelId.Value, s.Method))
            .ShouldBe(
            [
                (1, HardcodedAdvertisementChannelRepository.TestChannelId.Value, AdvertisementMethod.SingleHotRole),
                (3, HardcodedAdvertisementChannelRepository.TestChannelId.Value, AdvertisementMethod.NewlyOpenedProjectsDigest),
                (2, HardcodedAdvertisementChannelRepository.ZovemChannelId.Value, AdvertisementMethod.SingleHotRole),
                (4, HardcodedAdvertisementChannelRepository.ZovemChannelId.Value, AdvertisementMethod.NewlyOpenedProjectsDigest),
            ], ignoreOrder: true);
    }
}
