using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

public class SearchController : Common.LegacyJoinControllerBase
{
    private readonly ISearchService _searchService;
    private readonly IProjectRepository _projectRepository;
    private IUriService UriService { get; }

    public async Task<ActionResult> Index(string? searchString)
    {
        var searchResults =
            string.IsNullOrEmpty(searchString)
                ? []
                : await _searchService.SearchAsync(CurrentUserIdOrDefault, searchString);

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
                searchString ?? "",
                searchResults,
                projectDetails,
                UriService));
    }

    public SearchController(
        ISearchService searchService,
        IProjectRepository projectRepository,
        IUriService uriService,
        IUserRepository userRepository) : base(userRepository)
    {
        _searchService = searchService;
        _projectRepository = projectRepository;
        UriService = uriService;
    }
}
