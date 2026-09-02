using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes.Advertisement;

[method: JsonConstructor]
[TypedEntityId(ShortName = "AdvChannel")]
public partial record AdvertisementChannelIdentification(int Value);

public record AdvertisementChannelInfo(
    AdvertisementChannelIdentification ChannelId,
    string Name,
    ProjectIdentification? BoundProjectId, // null = глобальный канал
    AdvertisementChannelSettings Settings,
    bool IsActive = true);

[method: JsonConstructor]
[TypedEntityId(ShortName = "AdvSchedule")]
public partial record AdvertisementScheduleIdentification(int Value);

public enum AdvertisementMethod
{
    SingleHotRole = 1,
}

public record AdvertisementScheduleInfo(
    AdvertisementScheduleIdentification ScheduleId,
    AdvertisementChannelInfo Channel,
    AdvertisementMethod Method,
    IReadOnlySet<DayOfWeek> Days,
    bool IsActive = true)
{
    /// <summary>
    /// Расписание фактически активно, только если активно и оно само, и его канал.
    /// </summary>
    public bool IsEffectivelyActive => IsActive && Channel.IsActive;
}

public enum AdvertisementLogStatus
{
    Sent = 1,
    Failed = 2,
}

public record AdvertisementLogEntryInfo(
    AdvertisementScheduleIdentification ScheduleId,
    AdvertisementMethod Method,
    ProjectIdentification ProjectId,
    CharacterIdentification CharacterId,
    AdvertisementLogStatus Status,
    DateTimeOffset SentAt);

public record ProjectAdvertisementCandidate(
    ProjectIdentification ProjectId,
    int ActiveClaimsCount,
    int AdvertisementCount);
