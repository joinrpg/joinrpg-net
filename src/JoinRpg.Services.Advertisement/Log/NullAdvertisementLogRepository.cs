namespace JoinRpg.Services.Advertisement.Log;

// TODO: заменить на БД-backed реализацию — один EF-запрос, джойнящий горячих персонажей проекта
// с таблицей AdvertisementLog (ADR010 §4), без обращения к IHotCharactersRepository.
internal class NullAdvertisementLogRepository(IHotCharactersRepository hotCharactersRepository) : IAdvertisementLogRepository
{
    public async Task<IReadOnlyCollection<CharacterAdvertisementInfo>> GetHotCharactersAdvertisementInfo(
        AdvertisementScheduleIdentification scheduleId, ProjectIdentification projectId)
    {
        var hotCharacters = await hotCharactersRepository.GetHotCharactersFromPublicProjects();
        return [.. hotCharacters
            .Where(c => c.CharacterId.ProjectId == projectId)
            .Select(c => new CharacterAdvertisementInfo(c, AdvertisementCount: 0, AlreadySentForSchedule: false))];
    }

    public Task RecordAdvertisement(AdvertisementLogEntryInfo entry) => Task.CompletedTask;
}
