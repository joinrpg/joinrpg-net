using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Advertisement;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

public class AdvertisementEntityMapperTest
{
    [Fact]
    public void MaskToDays_Zero_ReturnsEmptySet() =>
        AdvertisementEntityMapper.MaskToDays(0).ShouldBeEmpty();

    [Fact]
    public void MaskToDays_AllBitsSet_ReturnsAllSevenDays() =>
        AdvertisementEntityMapper.MaskToDays(127).OrderBy(day => day)
            .ShouldBe(Enum.GetValues<DayOfWeek>());

    [Theory]
    [InlineData(0)]
    [InlineData(2)] // Monday
    [InlineData(8)] // Wednesday
    [InlineData(127)]
    public void DaysMask_RoundTrip_ReturnsSameMask(int mask) =>
        AdvertisementEntityMapper.DaysToMask(AdvertisementEntityMapper.MaskToDays(mask)).ShouldBe(mask);

    // Точные литералы бэкфилла миграции (задача 3): формат запинан, менять нельзя.
    [Theory]
    [InlineData(-1004315256401)]
    [InlineData(-1002544815071)]
    public void DeserializeSettings_TelegramKind_ReadsBackfillLiteral(long chatId)
    {
        var settings = AdvertisementEntityMapper.DeserializeSettings(
            1, $"{{\"ChatId\":\"TelegramChatId({chatId})\"}}");

        settings.ShouldBe(new TelegramChannelSettings(new TelegramChatId(chatId)));
    }

    [Fact]
    public void Settings_SerializeDeserialize_RoundTrip()
    {
        var original = new TelegramChannelSettings(new TelegramChatId(-1002544815071));

        var restored = AdvertisementEntityMapper.DeserializeSettings(
            1, AdvertisementEntityMapper.SerializeSettings(original));

        restored.ShouldBe(original);
    }

    [Fact]
    public void DeserializeSettings_UnknownKind_Throws() =>
        Should.Throw<InvalidOperationException>(() => AdvertisementEntityMapper.DeserializeSettings(99, "{}"));

    [Fact]
    public void ToInfo_Channel_MapsAllFields()
    {
        var entity = new AdvertisementChannelEntity
        {
            AdvertisementChannelId = 5,
            Name = "Телеграм-канал",
            ProjectId = 42,
            Kind = 1,
            SettingsJson = "{\"ChatId\":\"TelegramChatId(-1004315256401)\"}",
            IsActive = false,
        };

        var info = AdvertisementEntityMapper.ToInfo(entity);

        info.ShouldBe(new AdvertisementChannelInfo(
            new AdvertisementChannelIdentification(5),
            "Телеграм-канал",
            new JoinRpg.DomainTypes.ProjectIdentification(42),
            new TelegramChannelSettings(new TelegramChatId(-1004315256401)),
            IsActive: false));
    }

    [Fact]
    public void ToInfo_ChannelWithoutProject_BoundProjectIdIsNull()
    {
        var entity = new AdvertisementChannelEntity
        {
            AdvertisementChannelId = 1,
            Name = "Глобальный",
            ProjectId = null,
            Kind = 1,
            SettingsJson = "{\"ChatId\":\"TelegramChatId(-1004315256401)\"}",
            IsActive = true,
        };

        AdvertisementEntityMapper.ToInfo(entity).BoundProjectId.ShouldBeNull();
    }

    [Fact]
    public void ToInfo_ScheduleWithoutRegionsFilter_MapsNullAndNestedChannel()
    {
        var entity = CreateScheduleEntity(regionsFilter: null);

        var info = AdvertisementEntityMapper.ToInfo(entity);

        info.RegionsFilter.ShouldBeNull();
        info.ScheduleId.ShouldBe(new AdvertisementScheduleIdentification(7));
        info.Method.ShouldBe(AdvertisementMethod.SingleHotRole);
        info.Days.ShouldBe([DayOfWeek.Monday, DayOfWeek.Wednesday]);
        info.IsActive.ShouldBeTrue();
        info.Channel.ShouldBe(AdvertisementEntityMapper.ToInfo(entity.Channel));
    }

    [Fact]
    public void ToInfo_ScheduleWithRegionsFilter_MapsValue()
    {
        var entity = CreateScheduleEntity(regionsFilter: "Москва, СПб");

        AdvertisementEntityMapper.ToInfo(entity).RegionsFilter.ShouldBe("Москва, СПб");
    }

    private static AdvertisementScheduleEntity CreateScheduleEntity(string? regionsFilter) =>
        new()
        {
            AdvertisementScheduleId = 7,
            AdvertisementChannelId = 3,
            Channel = new AdvertisementChannelEntity
            {
                AdvertisementChannelId = 3,
                Name = "Канал",
                Kind = 1,
                SettingsJson = "{\"ChatId\":\"TelegramChatId(-1004315256401)\"}",
                IsActive = true,
            },
            Method = (int)AdvertisementMethod.SingleHotRole,
            DaysOfWeekMask = 2 | 8, // Monday + Wednesday
            RegionsFilter = regionsFilter,
            IsActive = true,
        };
}
