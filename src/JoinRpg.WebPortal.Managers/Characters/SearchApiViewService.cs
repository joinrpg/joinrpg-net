using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.WebPortal.Managers.Characters;

internal class SearchApiViewService(
    ISearchService searchService,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor
        ) : ISearchApiViewService
{
    public async Task<IReadOnlyCollection<CharacterSearchResult>> SearchCharacters(ProjectIdentification projectId, string query)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        _ = projectInfo.RequestMasterAccess(currentUserAccessor);

        var results = await searchService.SearchAsync(currentUserAccessor.UserIdOrDefault, query);

        return [.. results
            .Where(r => r.LinkType == LinkType.ResultCharacter && r.ProjectId == projectId.Value)
            .Select(r => new CharacterSearchResult
            {
                CharacterId = int.Parse(r.Identification),
                CharacterName = r.Name,
                IsPerfectMatch = r.IsPerfectMatch,
            })];
    }
}
