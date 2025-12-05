using System.Diagnostics.CodeAnalysis;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.Common;

[TypeFilter<CaptureNoAccessExceptionFilter>]
[DiscoverProjectFilter]
[Obsolete("Use JoinControllerGameBase")]
public abstract class ControllerGameBase(
    IProjectRepository projectRepository,
    IProjectService projectService) : LegacyJoinControllerBase
{
    protected IProjectService ProjectService { get; } = projectService;
    public IProjectRepository ProjectRepository { get; } = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));

    [DoesNotReturn]
    protected ActionResult NoAccesToProjectView(Project project) => throw new NoAccessToProjectException(project, CurrentUserId);

    protected ActionResult RedirectToIndex(Project project) => RedirectToAction("Index", "GameGroups", new { project.ProjectId, area = "" });

    protected ActionResult RedirectToIndex(int projectId, int characterGroupId, string action = "Index") => RedirectToAction(action, "GameGroups", new { projectId, characterGroupId, area = "" });

    protected ActionResult RedirectToIndex(CharacterGroupIdentification characterGroupId, string action = "Index") => RedirectToAction(action, "GameGroups", new { characterGroupId.ProjectId, characterGroupId.CharacterGroupId, area = "" });

    protected async Task<ActionResult> RedirectToProject(int projectId)
    {
        var project = await ProjectRepository.GetProjectAsync(projectId);
        return project == null ? NotFound() : RedirectToIndex(project);
    }
}

[TypeFilter<CaptureNoAccessExceptionFilter>]
[DiscoverProjectFilter]
public abstract class JoinControllerGameBase : ControllerBase
{
    protected ActionResult RedirectToIndex(Project project) => RedirectToAction("Index", "GameGroups", new { project.ProjectId, area = "" });

    protected ActionResult RedirectToIndex(int projectId, int characterGroupId, string action = "Index") => RedirectToAction(action, "GameGroups", new { projectId, characterGroupId, area = "" });

    protected ActionResult RedirectToIndex(CharacterGroupIdentification characterGroupId, string action = "Index") => RedirectToAction(action, "GameGroups", new { characterGroupId.ProjectId, characterGroupId.CharacterGroupId, area = "" });


    [DoesNotReturn]
    protected ActionResult NoAccesToProjectView(ProjectInfo project, ICurrentUserAccessor currentUserAccessor)
        => throw new NoAccessToProjectException(project, currentUserAccessor.UserIdOrDefault);
}
