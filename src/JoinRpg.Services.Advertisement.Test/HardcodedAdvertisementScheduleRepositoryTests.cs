using JoinRpg.Services.Advertisement.Channels;
using JoinRpg.Services.Advertisement.Schedules;

namespace JoinRpg.Services.Advertisement.Test;

public class HardcodedAdvertisementScheduleRepositoryTests
{
    [Fact]
    public async Task GetActiveSchedules_ReturnsScheduleForHotRoleChannelEveryDay()
    {
        var repository = new HardcodedAdvertisementScheduleRepository(new HardcodedAdvertisementChannelRepository());

        var schedules = await repository.GetActiveSchedules();

        var schedule = schedules.Single(s => s.Channel.ChannelId == HardcodedAdvertisementChannelRepository.HotRoleChannelId);
        schedule.Method.ShouldBe(AdvertisementMethod.SingleHotRole);
        schedule.Days.ShouldBe(Enum.GetValues<DayOfWeek>(), ignoreOrder: true);
    }

    [Fact]
    public async Task GetActiveSchedules_ReturnsScheduleForZovemChannelOnWednesdays()
    {
        var repository = new HardcodedAdvertisementScheduleRepository(new HardcodedAdvertisementChannelRepository());

        var schedules = await repository.GetActiveSchedules();

        var schedule = schedules.Single(s => s.Channel.ChannelId == HardcodedAdvertisementChannelRepository.ZovemChannelId);
        schedule.Method.ShouldBe(AdvertisementMethod.SingleHotRole);
        schedule.Days.ShouldBe([DayOfWeek.Wednesday], ignoreOrder: true);
    }
}
