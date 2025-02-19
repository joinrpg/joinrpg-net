using System.Diagnostics.CodeAnalysis;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Projects;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.Common;

[TypeFilter<CaptureNoAccessExceptionFilter>]
[DiscoverProjectFilter]
public abstract class ControllerGameBase : LegacyJoinControllerBase
{
    protected IProjectService ProjectService { get; }
    public IProjectRepository ProjectRepository { get; }

    protected ControllerGameBase(
        IProjectRepository projectRepository,
        IProjectService projectService,
        IUserRepository userRepository
        ) : base(userRepository)
    {
        ProjectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        ProjectService = projectService;
    }

    [DoesNotReturn]
    protected ActionResult NoAccesToProjectView(Project project) => throw new NoAccessToProjectException(project, CurrentUserId);

    [Obsolete]
    protected async Task<Project> GetProjectFromList(int projectId, IEnumerable<IProjectEntity> folders) => folders.FirstOrDefault()?.Project ?? await ProjectRepository.GetProjectAsync(projectId);


    protected ActionResult RedirectToIndex(Project project) => RedirectToAction("Index", "GameGroups", new { project.ProjectId, area = "" });

    protected ActionResult RedirectToIndex(int projectId, int characterGroupId, string action = "Index") => RedirectToAction(action, "GameGroups", new { projectId, characterGroupId, area = "" });

    protected ActionResult RedirectToRoles(CharacterGroupIdentification characterGroupId, string action = "Index") => RedirectToIndex(characterGroupId.ProjectId, characterGroupId.CharacterGroupId, action);

    protected async Task<ActionResult> RedirectToProject(int projectId)
    {
        var project = await ProjectRepository.GetProjectAsync(projectId);
        return project == null ? NotFound() : RedirectToIndex(project);
    }
}
