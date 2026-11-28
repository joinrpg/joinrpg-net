using System.Text.Json;
using JoinRpg.DomainTypes.Advertisement;
using JoinRpg.DomainTypes.Posting;

namespace JoinRpg.Dal.Impl.Repositories;

/// <summary>
/// Маппинг сущностей рекламных каналов/расписаний (ADR010 §2) в доменные типы.
/// </summary>
internal static class AdvertisementEntityMapper
{
    public static PostingChannelInfo ToInfo(AdvertisementChannelEntity entity) =>
        new(
            new PostingChannelIdentification(entity.AdvertisementChannelId),
            entity.Name,
            ProjectIdentification.FromOptional(entity.ProjectId),
            DeserializeSettings(entity.Kind, entity.SettingsJson),
            entity.IsActive);

    public static AdvertisementScheduleInfo ToInfo(AdvertisementScheduleEntity entity) =>
        new(
            new AdvertisementScheduleIdentification(entity.AdvertisementScheduleId),
            ToInfo(entity.Channel),
            entity.Method,
            MaskToDays(entity.DaysOfWeekMask),
            entity.IsActive,
            AdvertisementRegionsFilter.Parse(entity.RegionsFilter));

    private static HashSet<DayOfWeek> MaskToDays(int mask) =>
        Enum.GetValues<DayOfWeek>()
            .Where(day => (mask & (1 << (int)day)) != 0)
            .ToHashSet();

    private static TelegramChannelSettings DeserializeSettings(PostingChannelKind kind, string json) =>
        kind switch
        {
            PostingChannelKind.Telegram => JsonSerializer.Deserialize<TelegramChannelSettings>(json)!,
            _ => throw new InvalidOperationException($"Неизвестный Kind канала рекламы: {kind}"),
        };
}
