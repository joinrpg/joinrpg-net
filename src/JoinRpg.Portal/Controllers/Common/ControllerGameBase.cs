using System.Diagnostics.CodeAnalysis;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.Common;

[TypeFilter<CaptureNoAccessExceptionFilter>]
[DiscoverProjectFilter]
public abstract class JoinControllerGameBase : JoinMvcControllerBase
{
    protected ActionResult RedirectToIndex(Project project) => RedirectToAction("Index", "GameGroups", new { project.ProjectId, area = "" });

    protected ActionResult RedirectToIndex(int projectId, int characterGroupId, string action = "Index") => RedirectToAction(action, "GameGroups", new { projectId, characterGroupId, area = "" });

    protected ActionResult RedirectToIndex(CharacterGroupIdentification characterGroupId, string action = "Index") => RedirectToAction(action, "GameGroups", new { characterGroupId.ProjectId, characterGroupId.CharacterGroupId, area = "" });


    [DoesNotReturn]
    protected ActionResult NoAccesToProjectView(ProjectInfo project, ICurrentUserAccessor currentUserAccessor)
        => throw new NoAccessToProjectException(project, currentUserAccessor.UserIdOrDefault);
}
