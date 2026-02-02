using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Games.Projects;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Areas.Admin.Controllers;

[AdminAuthorize]
[Area("Admin")]
public class ProjectListController(
    IProjectRepository projectRepository,
    ICurrentUserAccessor currentUserAccessor) : Portal.Controllers.Common.ControllerBase
{
    public ICurrentUserAccessor CurrentUserAccessor { get; } = currentUserAccessor;

    public async Task<IActionResult> Index()
    {
        var allProjects = await projectRepository.GetPersonalizedProjectsBySpecification(CurrentUserAccessor.UserIdentification, ProjectListSpecification.All);

        var projects = allProjects.OrderByDisplayPriority().Select(p => new ProjectListItemViewModel(p)).ToList();

        return View(projects);
    }
}
