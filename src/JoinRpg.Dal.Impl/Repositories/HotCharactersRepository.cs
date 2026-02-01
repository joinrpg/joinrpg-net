using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

internal class HotCharactersRepository(MyDbContext ctx) : IHotCharactersRepository
{
    public async Task<IReadOnlyCollection<CharacterWithProject>> GetHotCharactersFromPublicProjects(KeySetPagination? pagination = null)
    {
        var query = ctx.ProjectsSet
            .AsExpandable()
            .Where(ProjectPredicates.Status(ProjectLifecycleStatus.ActiveClaimsOpen))
            .Where(ProjectPredicates.Public())
            .SelectMany(c => c.Characters)
            .Where(CharacterPredicates.Hot())
            .ApplyPaginationEf(pagination, c => c.CharacterId)
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
                KogdaIgraIds = c.Project.KogdaIgraGames.Select(k => k.KogdaIgraGameId)
            });

        return [..(await query.ToListAsync())
            .Select(c => new CharacterWithProject(
                new CharacterIdentification(c.ProjectId, c.CharacterId),
                c.CharacterName,
                c.IsPublic,
                c.IsActive,
                new ProjectName(c.ProjectName),
                c.CharacterDesc,
                c.ProjectDesc,
                [..c.KogdaIgraIds.Select(k => new KogdaIgraIdentification(k))]
                ))];
    }
}
