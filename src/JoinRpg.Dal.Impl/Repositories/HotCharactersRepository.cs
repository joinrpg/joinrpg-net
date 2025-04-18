using System.Data.Entity;
using JoinRpg.Data.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

internal class HotCharactersRepository(MyDbContext ctx) : RepositoryImplBase(ctx), IHotCharactersRepository
{
    public async Task<IReadOnlyCollection<CharacterWithProject>> GetHotCharactersFromAllProjects(KeySetPagination<CharacterIdentification>? pagination = null)
    {
        var query = Ctx.ProjectsSet
            .AsExpandable()
            .Where(ProjectPredicates.Status(ProjectLifecycleStatus.ActiveClaimsOpen))
            .SelectMany(c => c.Characters)
            .Where(CharacterPredicates.Hot())
            .Apply(pagination, c => c.CharacterId)
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
            });

        return [..(await query.ToListAsync())
            .Select(c => new CharacterWithProject(
                new CharacterIdentification(c.ProjectId, c.CharacterId),
                c.CharacterName,
                c.IsPublic,
                c.IsActive,
                new ProjectName(c.ProjectName),
                c.CharacterDesc,
                c.ProjectDesc))];
    }
}
