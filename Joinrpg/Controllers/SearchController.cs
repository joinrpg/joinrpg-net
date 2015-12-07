using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  public class SearchController : Common.ControllerBase
  {
    private readonly ISearchService _searchService ;
   
    public async Task<ActionResult> Index(SuperSearchViewModel viewModel)
    {
      var searchResults = await _searchService.SearchAsync(CurrentUserIdOrDefault, viewModel.SearchRequest);
      return searchResults.Count == 1
        ? RedirectToAction(searchResults.SingleOrDefault().AsObjectLink().GetRouteTarget())
        : View(new SearchResultViewModel {Results = searchResults, SearchString = viewModel.SearchRequest});
    }

    public SearchController(ApplicationUserManager userManager, ISearchService searchService) : base(userManager)
    {
      _searchService = searchService;
    }
  }
}