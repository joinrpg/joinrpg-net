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
    private IProjectRepository ProjectRepository { get; }

    protected ControllerGameBase(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService) : base(userManager)
    {
      ProjectRepository = projectRepository;
      ProjectService = projectService;
    }

    protected ActionResult WithProject(int projectId, Func<Project, ProjectAcl, ActionResult> action)
    {
      var project = ProjectRepository.GetProject(projectId);
      if (project == null)
      {
        return HttpNotFound();
      }
      var acl = Request.IsAuthenticated ? project.ProjectAcls.SingleOrDefault(a => a.UserId == CurrentUserId) : null;
      return action(project, acl);
    }

    protected ActionResult WithProject(int projectId, Func<Project, ActionResult> action)
    {
      return WithProject(projectId, (project, acl) => action(project));
    }

    protected ActionResult WithProjectAsMaster(int projectId, Func<ProjectAcl,bool> requiredRights, Func<Project, ActionResult> action)
    {
      return WithProject(projectId, (project, acl) =>
      {
        if (acl == null || requiredRights(acl))
        {
          return View("ErrorNoAccessToProject", project.ProjectName);
        }
        return action(project);
      });
    }

    protected ActionResult WithProjectAsMaster(int projectId, Func<Project, ActionResult> action)
    {
      return WithProjectAsMaster(projectId, acl => true, action);
    }

    protected ActionResult WithSubEntityAsMaster<TProjectSubEntity>(int projectId, int fieldId,
      Func<Project, IEnumerable<TProjectSubEntity>> subentitySelector, Func<TProjectSubEntity, int> subentityKeySelector,
      Func<ProjectAcl, bool> requiredRights, Func<Project, TProjectSubEntity, ActionResult> action)
    {
      return WithProjectAsMaster(projectId, requiredRights, project =>
      {
        var field = subentitySelector(project).SingleOrDefault(e => subentityKeySelector(e) == fieldId);
        return field == null ? HttpNotFound() : action(project, field);
      });
    }

    protected ActionResult WithSubEntity<TProjectSubEntity>(int projectId, int fieldId,
      Func<Project, IEnumerable<TProjectSubEntity>> subentitySelector, Func<TProjectSubEntity, int> subentityKeySelector,
      Func<Project, TProjectSubEntity, ActionResult> action)
    {
      return WithProject(projectId, project =>
      {
        var field = subentitySelector(project).SingleOrDefault(e => subentityKeySelector(e) == fieldId);
        return field == null ? HttpNotFound() : action(project, field);
      });
    }

    protected ActionResult WithGroup(int projectId, int groupId, Func<Project, CharacterGroup, ActionResult> action)
    {
      return WithSubEntity(projectId, groupId, project => project.CharacterGroups,
        subentity => subentity.CharacterGroupId, action);
    }

    protected ActionResult WithCharacter(int projectId, int characterId, Func<Project, Character, ActionResult> action)
    {
      return WithSubEntity(projectId, characterId, project => project.Characters,
        subentity => subentity.CharacterId, action);
    }
  }
}
