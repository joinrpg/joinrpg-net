using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search.Providers;

internal class CharacterProvider(IUnitOfWork unitOfWork, IProjectMetadataRepository projectMetadataRepository)
    : WorldObjectProviderBase(projectMetadataRepository), IProjectScopedSearchProvider
{
    //keep longer strings first to please Regexp
    private static readonly string[] keysForPerfectMath = ["%персонаж", "персонаж",];

    public LinkType LinkType => LinkType.ResultCharacter;

    public async Task<IReadOnlyCollection<SearchResult>> SearchAsync(int? currentUserId, string searchString, ProjectIdentification? projectId)
    {
        (var characterIdToFind, var matchByIdIsPerfect) = SearchKeywordsResolver.TryGetId(searchString, keysForPerfectMath);

        //TODO we don't search anymore by description
        var query =
            unitOfWork.GetDbSet<Character>()
              .Where(c =>
                (c.CharacterId == characterIdToFind
                || c.CharacterName.Contains(searchString))
                && c.IsActive
              );

        query = query.FilterByProject(projectId, c => c.ProjectId);

        var results =
          await query
              .OrderByDescending(cg => cg.CharacterName.Contains(searchString))
              .ToListAsync();

        return await GetWorldObjectsResultAsync(
          currentUserId,
          results,
          LinkType.ResultCharacter,
          searchedEntityId: characterIdToFind,
          matchByIdIsPerfect: matchByIdIsPerfect);
    }
}
