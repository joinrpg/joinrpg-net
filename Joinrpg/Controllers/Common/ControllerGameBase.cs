using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers.Common
{
  public class ControllerGameBase : ControllerBase
  {
    protected const string GroupFieldPrefix = "group_";
    protected const string CharFieldPrefix = "char_";
    protected IProjectService ProjectService { get; }
    protected IProjectRepository ProjectRepository { get; }

    protected ControllerGameBase(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService) : base(userManager)
    {
      ProjectRepository = projectRepository;
      ProjectService = projectService;
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


    protected ActionResult WithEntity(IProjectEntity field)
    {
      if (field == null) return HttpNotFound();
      var project = field.Project;
      if (project == null)
      {
        return HttpNotFound();
      }
      if (project.HasMasterAccess(CurrentUserIdOrDefault))
      {
        ViewBag.MasterMenu = new MasterMenuViewModel
        {
          ProjectId = project.ProjectId,
          ProjectName = project.ProjectName,
          HasAllrpg = project.Details?.AllrpgId != null
        };
      }
      return null;
    }

    protected ActionResult WithMyClaim(Claim claim)
    {
      if (claim == null)
      {
        return HttpNotFound();
      }
      if (claim.PlayerUserId != CurrentUserId)
      {
        return NoAccesToProjectView(claim.Project);
      }
      return WithEntity(claim);
    }

    protected ActionResult WithClaim(Claim claim)
    {
      if (claim == null)
      {
        return HttpNotFound();
      }
      if (!claim.HasAccess(CurrentUserId))
      {
        return NoAccesToProjectView(claim.Project);
      }

      return WithEntity(claim);
    }

    protected static IDictionary<int,string> GetCharacterFieldValuesFromPost(Dictionary<string, string> post)
    {
      return GetDynamicValuesFromPost(post, CharacterFieldValue.HtmlIdPrefix);
    }

    protected ActionResult AsMaster<TEntity>(TEntity entity) where TEntity : IProjectEntity
    {
      return AsMaster(entity, acl => true);
    }

    protected ActionResult AsMaster<TEntity>(TEntity entity, Func<ProjectAcl, bool> requiredRights) where TEntity : IProjectEntity
    {
      return WithEntity(entity) ??
             (entity.HasMasterAccess(CurrentUserId, requiredRights)
               ? null
               : NoAccesToProjectView(entity.Project));
    }

    protected async Task<Project> GetProjectFromList(int projectId, IEnumerable<IProjectEntity> folders)
    {
      return folders.FirstOrDefault()?.Project ?? await ProjectRepository.GetProjectAsync(projectId);
    }


    protected ActionResult RedirectToIndex(Project project)
    {
      return RedirectToAction("Index", "GameGroups",
        new { project.ProjectId, project.CharacterGroups.Single(cg => cg.IsRoot).CharacterGroupId, area = "" });
    }

    protected async Task<ActionResult> RedirectToProject(int projectId)
    {
      var project1 = await ProjectRepository.GetProjectAsync(projectId);
      var errorResult = WithEntity(project1);
      if (errorResult != null)
      {
        return errorResult;
      }
      return RedirectToIndex(project1);
    }

    protected IEnumerable<T> LoadIfMaster<T>(Project project, Func<IEnumerable<T>> load)
    {
      return (User.Identity.IsAuthenticated && project.HasMasterAccess(CurrentUserId))
        ? load()
        : new T[] {};
    }
  }
}
