using System;
using JetBrains.Annotations;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Web.Models
{
  /// <summary>
  /// A view model for a single item from search result
  /// carrying, in additon, the target of the made search.
  /// </summary>
  public class TargetedSearchResultViewModel
  { 
    public string SearchTarget { get; private set; }
    public ISearchResult SearchResult { get; private set; }

    public TargetedSearchResultViewModel([NotNull] ISearchResult searchResult, [NotNull] string searchTarget)
    {
      if (searchResult == null)
      {
        throw new ArgumentNullException(nameof(searchResult));
      }
      if (searchTarget == null)
      {
        throw new ArgumentNullException(nameof(searchTarget));
      }
      SearchResult = searchResult;
      SearchTarget = searchTarget;
    }
  }
}