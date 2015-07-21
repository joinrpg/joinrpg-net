using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Controllers.Common
{
  public class ControllerGameBase : ControllerBase
  {
    protected IProjectService ProjectService { get; }
    protected IProjectRepository ProjectRepository { get; }

    protected ControllerGameBase(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService) : base(userManager)
    {
      ProjectRepository = projectRepository;
      ProjectService = projectService;
    }

    protected ActionResult InsideProject(int projectId, Func<ProjectAcl, bool> requiredRights,
      Func<Project, ActionResult> action)
    {
      var project = ProjectRepository.GetProject(projectId);
      var result = project.CheckAccess(CurrentUserId, requiredRights);
      if (!result)
      {
        return View("ErrorNoAccessToProject", project.ProjectName);
      }
      return action(project);
    }

    protected ActionResult InsideProject(int projectId, Func<Project, ActionResult> action)
    {
      return InsideProject(projectId, pa => true, action);
    }

    protected ActionResult InsideProjectSubentity<TProjectSubEntity>(int projectId, int fieldId,
      Func<Project, IEnumerable<TProjectSubEntity>> subentitySelector, Func<TProjectSubEntity, int> subentityKeySelector,
      Func<Project, TProjectSubEntity, ActionResult> action)
    {
      return InsideProject(projectId, pa => pa.CanChangeFields, project =>
      {
        var field = subentitySelector(project).SingleOrDefault(e => subentityKeySelector(e) == fieldId);
        return field == null ? HttpNotFound() : action(project, field);
      });
    }
  }
}
