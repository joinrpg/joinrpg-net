using System;
using System.Collections.Generic;
using System.IO;
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
    protected IExportDataService ExportDataService { get; }
    protected IProjectRepository ProjectRepository { get; }


    protected ControllerGameBase(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IExportDataService exportDataService) : base(userManager)
    {
      ProjectRepository = projectRepository;
      ProjectService = projectService;
      ExportDataService = exportDataService;
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
      var acl = project.ProjectAcls.FirstOrDefault(a => a.UserId == CurrentUserIdOrDefault);
      if (acl != null)
      {
        ViewBag.MasterMenu = new MasterMenuViewModel
        {
          ProjectId = project.ProjectId,
          ProjectName = project.ProjectName,
          HasAllrpg = project.Details?.AllrpgId != null,
          Masters = project.ProjectAcls.Select(a => a.User),
          AccessToProject = acl
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

    protected IDictionary<int,string> GetCharacterFieldValuesFromPost()
    {
      return GetDynamicValuesFromPost(CharacterFieldValue.HtmlIdPrefix);
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
      return RedirectToIndex(project.ProjectId, project.CharacterGroups.Single(cg => cg.IsRoot).CharacterGroupId);
    }

    protected ActionResult RedirectToIndex(int projectId, int characterGroupId)
    {
      return RedirectToAction("Index", "GameGroups", new {projectId, characterGroupId, area = ""});
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

    protected ExportType? GetExportTypeByName(string export)
    {
      switch (export)
      {
        case "csv": return ExportType.Csv;
        case "xlsx": return ExportType.ExcelXml;
        default: return null;
      }
    }

    protected async Task<FileContentResult> Export<T>(IEnumerable<T> @select, string fileName, ExportType exportType = ExportType.Csv)
    {
      var generator = ExportDataService.GetGenerator(exportType, @select).BindDisplay<User>(user => user.DisplayName);
      return File(await generator.Generate(), generator.ContentType, Path.ChangeExtension(fileName, generator.FileExtension));
    }
  }
}
