using System.Text.Json;
using JoinRpg.DomainTypes.Advertisement;

namespace JoinRpg.Dal.Impl.Repositories;

/// <summary>
/// Маппинг сущностей рекламных каналов/расписаний (ADR010 §2) в доменные типы.
/// </summary>
internal static class AdvertisementEntityMapper
{
    private const int TelegramKind = 1;

    public static AdvertisementChannelInfo ToInfo(AdvertisementChannelEntity entity) =>
        new(
            new AdvertisementChannelIdentification(entity.AdvertisementChannelId),
            entity.Name,
            entity.ProjectId.HasValue
                ? new ProjectIdentification(entity.ProjectId.Value)
                : null,
            DeserializeSettings(entity.Kind, entity.SettingsJson),
            entity.IsActive);

    public static AdvertisementScheduleInfo ToInfo(AdvertisementScheduleEntity entity) =>
        new(
            new AdvertisementScheduleIdentification(entity.AdvertisementScheduleId),
            ToInfo(entity.Channel),
            (AdvertisementMethod)entity.Method,
            MaskToDays(entity.DaysOfWeekMask),
            entity.IsActive,
            entity.RegionsFilter);

    public static int DaysToMask(IReadOnlySet<DayOfWeek> days) =>
        days.Aggregate(0, (mask, day) => mask | (1 << (int)day));

    public static IReadOnlySet<DayOfWeek> MaskToDays(int mask) =>
        Enum.GetValues<DayOfWeek>()
            .Where(day => (mask & (1 << (int)day)) != 0)
            .ToHashSet();

    public static string SerializeSettings(AdvertisementChannelSettings settings) =>
        settings switch
        {
            TelegramChannelSettings telegram => JsonSerializer.Serialize(telegram),
            _ => throw new InvalidOperationException($"Неизвестный тип настроек канала рекламы: {settings.GetType().Name}"),
        };

    public static AdvertisementChannelSettings DeserializeSettings(int kind, string json) =>
        kind switch
        {
            TelegramKind => JsonSerializer.Deserialize<TelegramChannelSettings>(json)!,
            _ => throw new InvalidOperationException($"Неизвестный Kind канала рекламы: {kind}"),
        };
}
