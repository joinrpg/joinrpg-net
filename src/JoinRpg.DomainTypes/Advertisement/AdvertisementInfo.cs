using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes.Advertisement;

[method: JsonConstructor]
[TypedEntityId(ShortName = "AdvChannel")]
public partial record AdvertisementChannelIdentification(int Value);

public record AdvertisementChannelInfo(
    AdvertisementChannelIdentification ChannelId,
    ProjectIdentification? BoundProjectId, // null = глобальный канал
    AdvertisementChannelSettings Settings);

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
    IReadOnlySet<DayOfWeek> Days);

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
