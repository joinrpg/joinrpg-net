using System; 
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;

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

    protected ActionResult WithProject(int projectId, Func<Project, ProjectAcl, ActionResult> action)
    {
      var project = ProjectRepository.GetProject(projectId);
      return WithProject(project) ?? action(project, project.GetProjectAcl(CurrentUserIdOrDefault));
    }

    protected ActionResult WithProject(Project project)
    {
      if (project == null)
      {
        return HttpNotFound();
      }
      var acl = project.GetProjectAcl(CurrentUserIdOrDefault);
      if (acl != null)
      {
        ViewBag.MasterMenu = new MasterMenuViewModel
        {
          ProjectId = project.ProjectId,
          ProjectName = project.ProjectName
        };
      }
      return null;
    }

    protected ActionResult AsMaster(Project project, Func<ProjectAcl, bool> requiredRights)
    {
      var projectError = WithProject(project);
      if (projectError != null)
      {
        return projectError;
      }
      var acl = project.GetProjectAcl(CurrentUserIdOrDefault);
      if (acl == null || !requiredRights(acl))
      {
        return NoAccesToProjectView(project);
      }
      return null;
    }

    protected ActionResult AsMaster(Project project)
    {
      return AsMaster(project, acl => true);
    }

    protected ActionResult WithProjectAsMaster(int projectId, Func<ProjectAcl,bool> requiredRights, Func<Project, ActionResult> action)
    {
      var project = ProjectRepository.GetProject(projectId);
      return AsMaster(project, requiredRights) ?? action(project);
    }

    private ActionResult NoAccesToProjectView(Project project)
    {
      return View("ErrorNoAccessToProject",
        new ErrorNoAccessToProjectViewModel
        {
          CanGrantAccess = project.ProjectAcls.Where(acl => acl.CanGrantRights).Select(acl => acl.User),
          ProjectId = project.ProjectId,
          ProjectName = project.ProjectName
        });
    }

    protected async Task<ActionResult> WithProjectAsMasterAsync(int projectId, Func<Project, ActionResult> action)
    {
      var project1 = await ProjectRepository.GetProjectAsync(projectId);
      return AsMaster(project1) ?? action(project1);
    }

    private ActionResult WithSubEntityAsMaster<TProjectSubEntity>(int projectId, int? fieldId,
      Func<Project, IEnumerable<TProjectSubEntity>> subentitySelector,
      Func<ProjectAcl, bool> requiredRights, Func<Project, TProjectSubEntity, ActionResult> action) where TProjectSubEntity : IProjectSubEntity
    {

      var project1 = ProjectRepository.GetProject(projectId);
      var field = subentitySelector(project1).SingleOrDefault(e => e.Id == fieldId);
      return AsMaster(field,requiredRights) ?? action(project1, field);
    }

    private ActionResult WithSubEntity<TProjectSubEntity>(int projectId, int fieldId,
      Func<Project, IEnumerable<TProjectSubEntity>> subentitySelector,
      Func<Project, TProjectSubEntity, ActionResult> action) where TProjectSubEntity: IProjectSubEntity
    {
      var project1 = ProjectRepository.GetProject(projectId);
      var field = subentitySelector(project1).SingleOrDefault(e => e.Id == fieldId);
      return WithProject(field.Project) ?? action(project1, field);
    }

    protected ActionResult WithGroup(int projectId, int groupId, Func<Project, CharacterGroup, ActionResult> action)
    {
      var field = ProjectRepository.GetCharacterGroup(projectId, groupId);
      return WithProject(field.Project) ?? action(field.Project, field);
    }

    protected ActionResult WithCharacter(int projectId, int characterId, Func<Project, Character, ActionResult> action)
    {
      return WithSubEntity(projectId, characterId, project => project.Characters, action);
    }

    protected ActionResult WithClaim(int projectId, int claimId, Func<Project, Claim, bool, bool, ActionResult> actionResult)
    {
      return WithSubEntity(projectId, claimId, project => project.Claims, (project, claim) =>
      {
        if (!claim.HasAccess(CurrentUserId))
        {
          return NoAccesToProjectView(project);
        }
        return actionResult(project, claim, project.HasAccess(CurrentUserId), claim.PlayerUserId == CurrentUserId);
      });
    }

    protected ActionResult WithClaimAsMaster(int projectId, int claimId, Func<Project, Claim, ActionResult> actionResult)
    {
      return WithClaim(projectId, claimId, (project, claim, hasMasterAccess, isMyClaim) =>
      {
        if (!hasMasterAccess)
        {
          return NoAccesToProjectView(project);
        }
        return actionResult(project, claim);
      });
    }

    protected ActionResult WithMyClaim (int projectId, int claimId, Func<Project, Claim, ActionResult> actionResult)
    {
      return WithClaim(projectId, claimId, (project, claim, hasMasterAccess, isMyClaim) =>
      {
        if (!isMyClaim)
        {
          return NoAccesToProjectView(project);
        }
        return actionResult(project, claim);
      });
    }

    protected ActionResult WithGroupAsMaster(int projectId, int? groupId, Func<Project, CharacterGroup, ActionResult> action)
    {
      return WithSubEntityAsMaster(projectId, groupId, project => project.CharacterGroups, pa => pa.CanChangeFields, action);
    }

    protected ActionResult WithGameFieldAsMaster(int projectId, int fieldId,
      Func<Project, ProjectCharacterField, ActionResult> action)
    {
      return WithSubEntityAsMaster(projectId, fieldId, project => project.AllProjectFields, pa => pa.CanChangeFields, action);
    }

    protected ActionResult RedirectToIndex(Project project)
    {
      return RedirectToAction("Index", "GameGroups",
        new {project.ProjectId, project.CharacterGroups.Single(cg => cg.IsRoot).CharacterGroupId});
    }

    protected static IDictionary<int,string> GetCharacterFieldValuesFromPost(Dictionary<string, string> post)
    {
      var prefix = "field.field_";
      return GetDynamicValuesFromPost(post, prefix);
    }

    protected ActionResult AsMaster<TEntity>(TEntity entity) where TEntity : IProjectSubEntity
    {
      return AsMaster(entity.Project, acl => true);
    }

    protected ActionResult AsMaster<TEntity>(TEntity entity, Func<ProjectAcl, bool> requiredRights) where TEntity : IProjectSubEntity
    {
      return AsMaster(entity.Project, requiredRights);
    }
  }
}
