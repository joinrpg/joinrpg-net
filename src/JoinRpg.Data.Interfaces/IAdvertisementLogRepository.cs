namespace JoinRpg.Data.Interfaces;

public interface IAdvertisementLogRepository
{
    /// <summary>
    /// Горячие персонажи проекта вместе с их статистикой по рекламе в рамках указанного расписания.
    /// </summary>
    Task<IReadOnlyCollection<CharacterAdvertisementInfo>> GetHotCharactersAdvertisementInfo(
        AdvertisementScheduleIdentification scheduleId, ProjectIdentification projectId);

    /// <summary>
    /// true, если проект встречается среди последних <paramref name="n"/> отправленных реклам
    /// в рамках расписания — используется для кулдауна повторной рекламы одного проекта в канале.
    /// </summary>
    Task<bool> WasProjectAdvertisedAmongLastN(
        AdvertisementScheduleIdentification scheduleId, ProjectIdentification projectId, int n);

    Task RecordAdvertisement(AdvertisementLogEntryInfo entry);
}

public record CharacterAdvertisementInfo(
    CharacterWithProject Character, int AdvertisementCount, bool AlreadySentForSchedule);
