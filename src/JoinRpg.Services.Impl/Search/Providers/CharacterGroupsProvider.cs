using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search;

internal class CharacterGroupsProvider : WorldObjectProviderBase, ISearchProvider
{
    private readonly IUnitOfWork unitOfWork;

    public CharacterGroupsProvider(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<SearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
        int? characterGroupIdToFind = int.TryParse(searchString.Trim(), out var parsedValue) ? parsedValue : null;

        var queryResults =
          await
            unitOfWork.GetDbSet<CharacterGroup>()
              .Where(cg =>
                (cg.CharacterGroupId == characterGroupIdToFind
                || cg.CharacterGroupName.Contains(searchString)
                || (cg.Description.Contents != null && cg.Description.Contents.Contains(searchString)))
                && cg.IsActive && !cg.IsRoot
              )
              .OrderByDescending(cg => cg.CharacterGroupName.Contains(searchString))
              .ToListAsync();

        //search by ID is only for masters of the group's project
        var characterGroups = queryResults.Where(cg =>
          CheckMasterAccessIfMatchById(cg, currentUserId, characterGroupIdToFind));

        return GetWorldObjectsResult(
          currentUserId,
          characterGroups,
          LinkType.ResultCharacterGroup,
          wasFoundByIdPredicate: cg => cg.Id == characterGroupIdToFind,
          perfectMatchPredicte: cg => false);
    }
}
