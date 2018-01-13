using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
    public class SearchController : Common.ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly IProjectRepository _projectRepository;
        private IUriService UriService { get; }

        public async Task<ActionResult> Index(SuperSearchViewModel viewModel)
        {
            var searchResults =
                await _searchService.SearchAsync(CurrentUserIdOrDefault, viewModel.SearchRequest);

            if (searchResults.Count == 1)
            {
                return Redirect(UriService.Get(searchResults.Single()));
            }

            var projectDetails =
                (await _projectRepository.GetAllProjectsWithClaimCount(CurrentUserIdOrDefault))
                .ToDictionary(
                    p => p.ProjectId,
                    p => new ProjectListItemViewModel(p));

            return View(
                new SearchResultViewModel(
                    viewModel.SearchRequest,
                    searchResults,
                    projectDetails,
                    UriService));
        }

        public SearchController(
            ApplicationUserManager userManager,
            ISearchService searchService,
            IProjectRepository projectRepository,
            IUriService uriService) : base(userManager)
        {
            _searchService = searchService;
            _projectRepository = projectRepository;
            UriService = uriService;
        }
    }
}
