using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Search;
using JoinRpg.Web.Games.Projects;

namespace JoinRpg.Web.Models;

public class SearchResultViewModel(string searchString, IEnumerable<SearchResult> results, IReadOnlyDictionary<ProjectIdentification, ProjectListItemViewModel> projectDetails, IUriService uriService)
{
    public string SearchString { get; } = searchString;
    public List<IGrouping<ProjectListItemViewModel?, TargetedSearchResultViewModel>> ResultsByProject { get; } = results.Select(r =>
          new TargetedSearchResultViewModel(r, searchString, uriService, r.ProjectId == null ? null : projectDetails[new(r.ProjectId.Value)]))
            .GroupBy(r => r.ProjectViewModel)
            .OrderByDescending(r => r.Key is null)
            .ThenByDescending(r => r.Key?.IsActive)
            .ThenByDescending(r => r.Key?.IsMaster)
            .ThenByDescending(r => r.Key?.HasMyClaims)
            .ThenByDescending(r => r.Key?.IsActive)
            .ToList();
}
