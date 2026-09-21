using JoinRpg.Services.Advertisement.Channels;
using JoinRpg.Services.Advertisement.Schedules;

namespace JoinRpg.Services.Advertisement.Test;

public class HardcodedAdvertisementScheduleRepositoryTests
{
    [Fact]
    public async Task GetActiveSchedules_ReturnsScheduleForTestChannelEveryDay()
    {
        var repository = new HardcodedAdvertisementScheduleRepository(new HardcodedAdvertisementChannelRepository());

        var schedules = await repository.GetActiveSchedules();

        var schedule = schedules.Single(s =>
            s.Channel.ChannelId == HardcodedAdvertisementChannelRepository.TestChannelId
            && s.Method == AdvertisementMethod.SingleHotRole);
        schedule.Days.ShouldBe(Enum.GetValues<DayOfWeek>(), ignoreOrder: true);
    }

    [Fact]
    public async Task GetActiveSchedules_ReturnsScheduleForZovemChannelOnWednesdays()
    {
        var repository = new HardcodedAdvertisementScheduleRepository(new HardcodedAdvertisementChannelRepository());

        var schedules = await repository.GetActiveSchedules();

        var schedule = schedules.Single(s =>
            s.Channel.ChannelId == HardcodedAdvertisementChannelRepository.ZovemChannelId
            && s.Method == AdvertisementMethod.SingleHotRole);
        schedule.Days.ShouldBe([DayOfWeek.Wednesday], ignoreOrder: true);
    }

    [Fact]
    public async Task GetActiveSchedules_ReturnsNewlyOpenedProjectsDigestForTestChannelOnMondays()
    {
        // Новый способ рекламы пока обкатывается только в тестовом канале (см. HardcodedAdvertisementScheduleRepository).
        var repository = new HardcodedAdvertisementScheduleRepository(new HardcodedAdvertisementChannelRepository());

        var schedules = await repository.GetActiveSchedules();

        var schedule = schedules.Single(s => s.Method == AdvertisementMethod.NewlyOpenedProjectsDigest);
        schedule.Channel.ChannelId.ShouldBe(HardcodedAdvertisementChannelRepository.TestChannelId);
        schedule.Days.ShouldBe([DayOfWeek.Monday], ignoreOrder: true);
    }

    [Fact]
    public async Task GetAllSchedules_ReturnsAllChannelsRegardlessOfActivity()
    {
        var repository = new HardcodedAdvertisementScheduleRepository(new HardcodedAdvertisementChannelRepository());

        var schedules = await repository.GetAllSchedules();

        schedules.Count.ShouldBe(3);
    }
}
