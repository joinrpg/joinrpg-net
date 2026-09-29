using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search.Providers;

internal class ClaimsByIdProvider(IUnitOfWork unitOfWork, IProjectMetadataRepository projectMetadataRepository) : IProjectScopedSearchProvider
{
    //keep longer strings first to please Regexp
    private static readonly string[] keysForPerfectMath = ["%заявка", "заявка",];

    public LinkType LinkType => LinkType.Claim;

    public async Task<IReadOnlyCollection<SearchResult>> SearchAsync(int? currentUserId, string searchString, ProjectIdentification? projectId)
    {
        (var idToFind, var matchByIdIsPerfect) = SearchKeywordsResolver.TryGetId(searchString, keysForPerfectMath);

        if (idToFind == null)
        {
            //Only search by Id is valid for claims
            return [];
        }

        var query =
            unitOfWork.GetDbSet<Claim>()
              // Имя берём из Character — подтягиваем сразу, чтобы не лениво догружать (#4991).
              .Include(claim => claim.Character)
              .Where(claim => claim.ClaimId == idToFind);

        query = query.FilterByProject(projectId, claim => claim.ProjectId);

        var results = await query.ToListAsync();

        var searchResults = new List<SearchResult>();
        foreach (var claim in results)
        {
            // Проверка доступа поверх ProjectInfo (кеш на запрос), а не claim.Project.ProjectAcls —
            // иначе на каждый найденный объект лениво догружаются Projects и ProjectAcls (#4991).
            var projectInfo = await projectMetadataRepository.GetProjectMetadata(claim.ProjectIdentification);
            if (!projectInfo.HasMasterAccess(UserIdentification.FromOptional(currentUserId)))
            {
                continue;
            }

            searchResults.Add(new SearchResult
            {
                LinkType = LinkType.Claim,
                Name = claim.Character.CharacterName,
                Description = SearchUtils.GetFoundByIdDescription(claim.ClaimId),
                Identification = claim.ClaimId.ToString(),
                ProjectId = claim.ProjectId,
                IsPublic = false,
                IsActive = claim.ClaimStatus.IsActive(),
                IsPerfectMatch = claim.ClaimId == idToFind && matchByIdIsPerfect,
            });
        }
        return searchResults;
    }
}
