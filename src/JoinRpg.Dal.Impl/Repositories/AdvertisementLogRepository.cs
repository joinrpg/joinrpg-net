using JoinRpg.DomainTypes.Advertisement;

namespace JoinRpg.Dal.Impl.Repositories;

internal class AdvertisementLogRepository(MyDbContext ctx) : IAdvertisementLogRepository
{
    public async Task<IReadOnlyCollection<CharacterAdvertisementInfo>> GetHotCharactersAdvertisementInfo(
        AdvertisementScheduleIdentification scheduleId, ProjectIdentification projectId)
    {
        // Статус/публичность проекта здесь не проверяем: projectId уже провалидирован
        // вызывающим кодом (SingleHotRoleAdvertisementJob.TryAdvertiseProject).
        var query = ctx.ProjectsSet
            .Where(p => p.ProjectId == projectId.Value)
            .SelectMany(p => p.Characters)
            .Where(CharacterPredicates.Hot())
            .Select(c => new
            {
                c.CharacterId,
                c.ProjectId,
                c.CharacterName,
                c.Project.ProjectName,
                CharacterDesc = c.Description,
                ProjectDesc = c.Project.Details.ProjectAnnounce,
                c.IsActive,
                c.IsPublic,
                KogdaIgraIds = c.Project.KogdaIgraGames.Select(k => k.KogdaIgraGameId),
                AdvertisementCount = ctx.AdvertisementLogEntriesSet.Count(e =>
                    e.CharacterId == c.CharacterId && e.Status == (int)AdvertisementLogStatus.Sent),
                AlreadySentForSchedule = ctx.AdvertisementLogEntriesSet.Any(e =>
                    e.CharacterId == c.CharacterId && e.ScheduleId == scheduleId.Value && e.Status == (int)AdvertisementLogStatus.Sent),
            });

        return [.. (await query.ToListAsync())
            .Select(c => new CharacterAdvertisementInfo(
                new CharacterWithProject(
                    new CharacterIdentification(c.ProjectId, c.CharacterId),
                    c.CharacterName,
                    c.IsPublic,
                    c.IsActive,
                    new ProjectName(c.ProjectName),
                    c.CharacterDesc,
                    c.ProjectDesc,
                    [.. c.KogdaIgraIds.Select(k => new KogdaIgraIdentification(k))]),
                c.AdvertisementCount,
                c.AlreadySentForSchedule))];
    }

    public async Task<bool> WasProjectAdvertisedAmongLastN(
        AdvertisementScheduleIdentification scheduleId, ProjectIdentification projectId, int n) =>
        await ctx.AdvertisementLogEntriesSet
            .Where(e => e.ScheduleId == scheduleId.Value && e.Status == (int)AdvertisementLogStatus.Sent)
            .OrderByDescending(e => e.AdvertisementLogEntryId)
            .Take(n)
            .AnyAsync(e => e.ProjectId == projectId.Value);

    public async Task RecordAdvertisement(AdvertisementLogEntryInfo entry)
    {
        _ = ctx.AdvertisementLogEntriesSet.Add(new AdvertisementLogEntryEntity
        {
            ScheduleId = entry.ScheduleId.Value,
            Method = (int)entry.Method,
            ProjectId = entry.ProjectId.Value,
            CharacterId = entry.CharacterId.CharacterId,
            Status = (int)entry.Status,
            SentAt = entry.SentAt,
        });
        await ctx.SaveChangesAsync();
    }
}
