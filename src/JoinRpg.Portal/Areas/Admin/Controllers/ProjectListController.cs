using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Areas.Admin.Controllers;

[AdminAuthorize]
[Area("Admin")]
public class ProjectListController : JoinRpg.Portal.Controllers.Common.ControllerBase
{
    public ICurrentUserAccessor CurrentUserAccessor { get; }
    private readonly IProjectRepository _projectRepository;

    public async Task<IActionResult> Index()
    {
        var allProjects = await _projectRepository.GetActiveProjectsWithClaimCount(CurrentUserAccessor.UserId);

        var projects =
            allProjects
                .Select(p => new ProjectListItemViewModel(p))
                .OrderByDescending(p => p.ClaimCount)
                .ToList();

        return View(projects);
    }

    public ProjectListController(
        IProjectRepository projectRepository,
        ICurrentUserAccessor currentUserAccessor)
    {
        CurrentUserAccessor = currentUserAccessor;
        _projectRepository = projectRepository;
    }
}
