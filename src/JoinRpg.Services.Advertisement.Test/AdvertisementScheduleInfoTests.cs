namespace JoinRpg.Services.Advertisement.Test;

public class AdvertisementScheduleInfoTests
{
    private static PostingChannelInfo Channel(bool isActive) =>
        new(new PostingChannelIdentification(1), Name: "Test", BoundProjectId: null, new TelegramChannelSettings(new TelegramChatId(-100)), IsActive: isActive);

    private static AdvertisementScheduleInfo Schedule(bool scheduleActive, bool channelActive) =>
        new(new AdvertisementScheduleIdentification(1), Channel(channelActive), AdvertisementMethod.SingleHotRole, Enum.GetValues<DayOfWeek>().ToHashSet(), IsActive: scheduleActive);

    [Fact]
    public void RegionsFilter_NotSpecified_DefaultsToNull() =>
        new AdvertisementScheduleInfo(
            new AdvertisementScheduleIdentification(1),
            Channel(isActive: true),
            AdvertisementMethod.SingleHotRole,
            new HashSet<DayOfWeek> { DayOfWeek.Monday })
            .RegionsFilter.ShouldBeNull();

    [Fact]
    public void RegionsFilter_Specified_ReturnsProvidedValue() =>
        new AdvertisementScheduleInfo(
            new AdvertisementScheduleIdentification(1),
            Channel(isActive: true),
            AdvertisementMethod.SingleHotRole,
            new HashSet<DayOfWeek> { DayOfWeek.Monday },
            RegionsFilter: "Москва, СПб")
            .RegionsFilter.ShouldBe("Москва, СПб");

    [Fact]
    public void IsEffectivelyActive_ActiveScheduleAndActiveChannel_ReturnsTrue() =>
        Schedule(scheduleActive: true, channelActive: true).IsEffectivelyActive.ShouldBeTrue();

    [Fact]
    public void IsEffectivelyActive_InactiveSchedule_ReturnsFalse() =>
        Schedule(scheduleActive: false, channelActive: true).IsEffectivelyActive.ShouldBeFalse();

    [Fact]
    public void IsEffectivelyActive_InactiveChannel_ReturnsFalse() =>
        Schedule(scheduleActive: true, channelActive: false).IsEffectivelyActive.ShouldBeFalse();
}
