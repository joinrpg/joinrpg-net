using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  public class SearchController : Common.ControllerBase
  {
    private readonly ISearchService _searchService;
    private readonly IProjectRepository _projectRepository;

    public async Task<ActionResult> Index(SuperSearchViewModel viewModel)
    {
      var searchResults = await _searchService.SearchAsync(CurrentUserIdOrDefault, viewModel.SearchRequest);

      Dictionary<int, ProjectListItemViewModel> projectDetails =
        (await _projectRepository.GetAllProjectsWithClaimCount())
        .ToDictionary(
          p => p.ProjectId,
          p => ProjectListItemViewModel.FromProject(p, CurrentUserIdOrDefault));

      return searchResults.Count == 1
        ? RedirectToAction(searchResults.Single().GetRouteTarget())
        : View(new SearchResultViewModel
               {
                  Results = searchResults,
                  SearchString = viewModel.SearchRequest,
                  ProjectDetails = new ReadOnlyDictionary<int, ProjectListItemViewModel>(projectDetails)
        });
    }

    public SearchController(
      ApplicationUserManager userManager,
      ISearchService searchService,
      IProjectRepository projectRepository) : base(userManager)
    {
      _searchService = searchService;
      _projectRepository = projectRepository;
    }
  }
}