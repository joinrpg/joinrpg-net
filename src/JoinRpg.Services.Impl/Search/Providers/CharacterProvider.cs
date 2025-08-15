using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search.Providers;

internal class CharacterProvider(IUnitOfWork unitOfWork) : WorldObjectProviderBase, ISearchProvider
{
    //keep longer strings first to please Regexp
    private static readonly string[] keysForPerfectMath = ["%персонаж", "персонаж",];

    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
        (var characterIdToFind, var matchByIdIsPerfect) = SearchKeywordsResolver.TryGetId(searchString, keysForPerfectMath);

        //TODO we don't search anymore by description
        var results =
          await
            unitOfWork.GetDbSet<Character>()
              .Where(c =>
                (c.CharacterId == characterIdToFind
                || c.CharacterName.Contains(searchString))
                && c.IsActive
              )
              .OrderByDescending(cg => cg.CharacterName.Contains(searchString))
              .ToListAsync();

        //search by ID is only for masters of the character's project
        var characters = results.Where(c =>
          CheckMasterAccessIfMatchById(c, currentUserId, characterIdToFind));

        return GetWorldObjectsResult(
          currentUserId,
          characters,
          LinkType.ResultCharacter,
          wasFoundByIdPredicate: c => c.Id == characterIdToFind,
          perfectMatchPredicte: c => c.Id == characterIdToFind && matchByIdIsPerfect);
    }
}
