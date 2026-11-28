using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Advertisement;
using JoinRpg.DomainTypes.Posting;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

public class AdvertisementEntityMapperTest
{
    [Fact]
    public void ToInfo_ScheduleWithZeroMask_ReturnsEmptyDays() =>
        AdvertisementEntityMapper.ToInfo(CreateScheduleEntity(daysOfWeekMask: 0)).Days.ShouldBeEmpty();

    [Fact]
    public void ToInfo_ScheduleWithAllBitsSet_ReturnsAllSevenDays() =>
        AdvertisementEntityMapper.ToInfo(CreateScheduleEntity(daysOfWeekMask: 127)).Days.OrderBy(day => day)
            .ShouldBe(Enum.GetValues<DayOfWeek>());

    [Theory]
    [InlineData(2, DayOfWeek.Monday)]
    [InlineData(8, DayOfWeek.Wednesday)]
    public void ToInfo_ScheduleMask_MapsSingleDay(int mask, DayOfWeek expectedDay) =>
        AdvertisementEntityMapper.ToInfo(CreateScheduleEntity(daysOfWeekMask: mask)).Days.ShouldBe([expectedDay]);

    // Точные литералы бэкфилла миграции (задача 3): формат запинан, менять нельзя.
    [Theory]
    [InlineData(-1004315256401)]
    [InlineData(-1002544815071)]
    public void ToInfo_ChannelWithBackfillSettingsLiteral_ReadsChatId(long chatId)
    {
        var entity = CreateChannelEntity(settingsJson: $"{{\"ChatId\":\"TelegramChatId({chatId})\"}}");

        var info = AdvertisementEntityMapper.ToInfo(entity);

        info.Settings.ShouldBe(new TelegramChannelSettings(new TelegramChatId(chatId)));
    }

    // Точный литерал бэкфилла миграции: SerializeSettings обязан писать в том же формате.
    [Fact]
    public void SerializeSettings_TelegramSettings_WritesBackfillCompatibleLiteral()
    {
        var settings = new TelegramChannelSettings(new TelegramChatId(-1004315256401));

        var json = AdvertisementEntityMapper.SerializeSettings(settings);

        json.ShouldBe("{\"ChatId\":\"TelegramChatId(-1004315256401)\"}");
        AdvertisementEntityMapper.ToInfo(CreateChannelEntity(settingsJson: json)).Settings.ShouldBe(settings);
    }

    [Fact]
    public void ToInfo_ChannelWithUnknownKind_Throws() =>
        Should.Throw<InvalidOperationException>(() =>
            AdvertisementEntityMapper.ToInfo(CreateChannelEntity(kind: (PostingChannelKind)99)));

    [Fact]
    public void ToInfo_Channel_MapsAllFields()
    {
        var entity = new AdvertisementChannelEntity
        {
            AdvertisementChannelId = 5,
            Name = "Телеграм-канал",
            ProjectId = 42,
            Kind = PostingChannelKind.Telegram,
            SettingsJson = "{\"ChatId\":\"TelegramChatId(-1004315256401)\"}",
            IsActive = false,
        };

        var info = AdvertisementEntityMapper.ToInfo(entity);

        info.ShouldBe(new PostingChannelInfo(
            new PostingChannelIdentification(5),
            "Телеграм-канал",
            new JoinRpg.DomainTypes.ProjectIdentification(42),
            new TelegramChannelSettings(new TelegramChatId(-1004315256401)),
            IsActive: false));
    }

    [Fact]
    public void ToInfo_ChannelWithoutProject_BoundProjectIdIsNull()
    {
        var entity = CreateChannelEntity();

        AdvertisementEntityMapper.ToInfo(entity).BoundProjectId.ShouldBeNull();
    }

    [Fact]
    public void ToInfo_ScheduleWithoutRegionsFilter_MapsNullAndNestedChannel()
    {
        var entity = CreateScheduleEntity(regionsFilter: null);

        var info = AdvertisementEntityMapper.ToInfo(entity);

        info.Regions.ShouldBeNull();
        info.ScheduleId.ShouldBe(new AdvertisementScheduleIdentification(7));
        info.Method.ShouldBe(AdvertisementMethod.SingleHotRole);
        info.Days.ShouldBe([DayOfWeek.Monday, DayOfWeek.Wednesday]);
        info.IsActive.ShouldBeTrue();
        info.Channel.ShouldBe(AdvertisementEntityMapper.ToInfo(entity.Channel));
    }

    [Fact]
    public void ToInfo_ScheduleWithRegionsFilter_ParsesRegionIds()
    {
        var entity = CreateScheduleEntity(regionsFilter: "1,3");

        AdvertisementEntityMapper.ToInfo(entity).Regions.ShouldBe([Region.SanktPeterburg, Region.Moskva]);
    }

    [Fact]
    public void ToInfo_ScheduleWithGarbageRegionsFilter_MapsEmptySet()
    {
        var entity = CreateScheduleEntity(regionsFilter: "abc");

        AdvertisementEntityMapper.ToInfo(entity).Regions.ShouldBeEmpty();
    }

    private static AdvertisementChannelEntity CreateChannelEntity(
        string settingsJson = "{\"ChatId\":\"TelegramChatId(-1004315256401)\"}",
        PostingChannelKind kind = PostingChannelKind.Telegram) =>
        new()
        {
            AdvertisementChannelId = 1,
            Name = "Глобальный",
            ProjectId = null,
            Kind = kind,
            SettingsJson = settingsJson,
            IsActive = true,
        };

    private static AdvertisementScheduleEntity CreateScheduleEntity(
        string? regionsFilter = null,
        int daysOfWeekMask = 2 | 8) => // Monday + Wednesday
        new()
        {
            AdvertisementScheduleId = 7,
            AdvertisementChannelId = 3,
            Channel = CreateChannelEntity(),
            Method = AdvertisementMethod.SingleHotRole,
            DaysOfWeekMask = daysOfWeekMask,
            RegionsFilter = regionsFilter,
            IsActive = true,
        };
}
