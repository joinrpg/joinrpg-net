using JoinRpg.Services.Advertisement.Channels;
using JoinRpg.Services.Advertisement.Schedules;

namespace JoinRpg.Services.Advertisement.Test;

public class HardcodedAdvertisementScheduleRepositoryTests
{
    [Fact]
    public async Task GetActiveSchedules_ReturnsSingleHotRoleScheduleForHardcodedChannel()
    {
        var repository = new HardcodedAdvertisementScheduleRepository(new HardcodedAdvertisementChannelRepository());

        var schedules = await repository.GetActiveSchedules();

        var schedule = schedules.ShouldHaveSingleItem();
        schedule.Channel.ChannelId.ShouldBe(HardcodedAdvertisementChannelRepository.HotRoleChannelId);
        schedule.Method.ShouldBe(AdvertisementMethod.SingleHotRole);
        schedule.Days.ShouldBe(Enum.GetValues<DayOfWeek>(), ignoreOrder: true);
    }
}
