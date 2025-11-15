using JoinRpg.Data.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Games.Projects;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

public class SearchController(
    ISearchService searchService,
    IProjectRepository projectRepository,
    IUriService uriService,
    ICurrentUserAccessor currentUserAccessor) : Common.ControllerBase
{
    public async Task<ActionResult> Index(string? searchString)
    {
        var searchResults =
            string.IsNullOrEmpty(searchString)
                ? []
                : await searchService.SearchAsync(currentUserAccessor.UserIdOrDefault, searchString);

        if (searchResults.Count == 1)
        {
            return Redirect(uriService.Get(searchResults.Single()));
        }



        var projectDetails =
            (await projectRepository.GetProjectsByIds(currentUserAccessor.UserIdentificationOrDefault,
            [.. searchResults.Select(x => ProjectIdentification.FromOptional(x.ProjectId)).WhereNotNull()]))
            .ToDictionary(
                p => p.ProjectId,
                p => new ProjectListItemViewModel(p));

        return View(
            new SearchResultViewModel(
                searchString ?? "",
                searchResults,
                projectDetails,
                uriService));
    }
}
