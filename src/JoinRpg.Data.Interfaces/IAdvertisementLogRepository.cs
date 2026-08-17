namespace JoinRpg.Data.Interfaces;

public interface IAdvertisementLogRepository
{
    /// <summary>
    /// Горячие персонажи проекта вместе с их статистикой по рекламе в рамках указанного расписания.
    /// </summary>
    Task<IReadOnlyCollection<CharacterAdvertisementInfo>> GetHotCharactersAdvertisementInfo(
        AdvertisementScheduleIdentification scheduleId, ProjectIdentification projectId);

    Task RecordAdvertisement(AdvertisementLogEntryInfo entry);
}

public record CharacterAdvertisementInfo(
    CharacterWithProject Character, int AdvertisementCount, bool AlreadySentForSchedule);
