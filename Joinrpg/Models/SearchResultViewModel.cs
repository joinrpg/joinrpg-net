using System.Collections.Generic;
using System.Linq;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Web.Models
{
  public class SearchResultViewModel
  {
    public string SearchString { get; private set; }
    public IOrderedEnumerable<IGrouping<int?, TargetedSearchResultViewModel>> ResultsByProject { get; private set; }
    public IReadOnlyDictionary<int, ProjectListItemViewModel> ProjectDetails { get; private set; }

    public SearchResultViewModel(
      string searchString,
      IEnumerable<ISearchResult> results,
      IReadOnlyDictionary<int, ProjectListItemViewModel> projectDetails)
    {
      SearchString = searchString;

      var targetedResults = results.Select(r =>
        new TargetedSearchResultViewModel(r, searchString));

      ResultsByProject = ProjectListItemViewModel
        .OrderByDisplayPriority(
          targetedResults.GroupBy(r => r.SearchResult.ProjectId),
          g => g.Key == null ? null : projectDetails[g.Key.Value])
        .OrderByDescending(x => x.Key == null);

      ProjectDetails = projectDetails;
    }
  }
}
