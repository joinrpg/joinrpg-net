using System.Collections.Generic;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Web.Models
{
  public class SearchResultViewModel
  {
    public string SearchString { get; set; }
    public IEnumerable<ISearchResult> Results { get; set; }
  }
}
