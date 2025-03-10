using System.Collections.ObjectModel;
using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search;

internal class ClaimsByIdProvider : ISearchProvider
{
    //keep longer strings first to please Regexp
    private static readonly string[] keysForPerfectMath =
    {
  "%заявка",
  "заявка",
};

    private readonly IUnitOfWork unitOfWork;

    public ClaimsByIdProvider(IUnitOfWork unitOfWork) => this.unitOfWork = unitOfWork;

    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
        (var idToFind, var matchByIdIsPerfect) = SearchKeywordsResolver.TryGetId(searchString, keysForPerfectMath);

        if (idToFind == null)
        {
            //Only search by Id is valid for claims
            return new ReadOnlyCollection<ISearchResult>(new List<ISearchResult>());
        }

        var results =
          await
            unitOfWork.GetDbSet<Claim>()
              .Where(claim => claim.ClaimId == idToFind)
              .ToListAsync();

        return results
          .Where(claim => claim.HasMasterAccess(currentUserId))
          .Select(claim => new SearchResultImpl
          {
              LinkType = LinkType.Claim,
              Name = claim.Character.CharacterName,
              Description = new MarkdownString(WorldObjectProviderBase.GetFoundByIdDescription(claim.ClaimId)),
              Identification = claim.ClaimId.ToString(),
              ProjectId = claim.ProjectId,
              IsPublic = false,
              IsActive = claim.ClaimStatus.IsActive(),
              IsPerfectMatch = claim.ClaimId == idToFind && matchByIdIsPerfect,
          })
          .ToList();
    }
}
