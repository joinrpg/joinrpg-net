using Microsoft.EntityFrameworkCore;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search;

internal class CharacterProvider : WorldObjectProviderBase, ISearchProvider
{
    //keep longer strings first to please Regexp
    private static readonly string[] keysForPerfectMath =
    {
  "%персонаж",
  "персонаж",
};

    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
        bool matchByIdIsPerfect;
        var characterIdToFind = SearchKeywordsResolver.TryGetId(
          searchString,
          keysForPerfectMath,
          out matchByIdIsPerfect);

        //TODO we don't search anymore by description
        var results =
          await
            UnitOfWork.GetDbSet<Character>()
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
