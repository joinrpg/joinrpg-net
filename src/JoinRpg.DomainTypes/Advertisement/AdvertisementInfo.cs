using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Posting;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Advertisement;

[method: JsonConstructor]
[TypedEntityId(ShortName = "AdvSchedule")]
public partial record AdvertisementScheduleIdentification(int Value);

public enum AdvertisementMethod
{
    SingleHotRole = 1,
    NewlyOpenedProjectsDigest = 2,
}

/// <param name="RegionsFilter">
/// Фильтр по регионам (текст). Пока нигде не используется — задел под фильтрацию по <c>KogdaIgraGameData.RegionName</c>.
/// </param>
public record AdvertisementScheduleInfo(
    AdvertisementScheduleIdentification ScheduleId,
    PostingChannelInfo Channel,
    AdvertisementMethod Method,
    IReadOnlySet<DayOfWeek> Days,
    bool IsActive = true,
    string? RegionsFilter = null)
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
    CharacterIdentification? CharacterId,
    AdvertisementLogStatus Status,
    DateTimeOffset SentAt);

public record ProjectAdvertisementCandidate(
    ProjectIdentification ProjectId,
    ProjectName ProjectName,
    int ActiveClaimsCount,
    int AdvertisementCount);
