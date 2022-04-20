using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Web.Models
{
    public class SearchResultViewModel
    {
        public string SearchString { get; }
        public IOrderedEnumerable<IGrouping<int?, TargetedSearchResultViewModel>> ResultsByProject { get; }
        public IReadOnlyDictionary<int, ProjectListItemViewModel> ProjectDetails { get; }

        public SearchResultViewModel(string searchString, IEnumerable<ISearchResult> results, IReadOnlyDictionary<int, ProjectListItemViewModel> projectDetails, IUriService uriService)
        {
            SearchString = searchString;

            var targetedResults = results.Select(r =>
              new TargetedSearchResultViewModel(r, searchString, uriService));

            ResultsByProject = ProjectListItemViewModel
              .OrderByDisplayPriority(
                targetedResults.GroupBy(r => r.SearchResult.ProjectId),
                g => g.Key == null ? null : projectDetails[g.Key.Value])
              .OrderByDescending(x => x.Key == null);

            ProjectDetails = projectDetails;
        }
    }
}
