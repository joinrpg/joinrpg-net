using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers.Common
{
  public class ControllerGameBase : ControllerBase
  {
    protected IProjectService ProjectService { get; }
    private IExportDataService ExportDataService { get; }
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

      ViewBag.ProjectId = project.ProjectId;

      var acl = project.ProjectAcls.FirstOrDefault(a => a.UserId == CurrentUserIdOrDefault);
      if (acl != null)
      {
        ViewBag.MasterMenu = new MasterMenuViewModel
        {
          ProjectId = project.ProjectId,
          ProjectName = project.ProjectName,
          AccessToProject = acl,
          BigGroups = project.RootGroup.ChildGroups.Where(cg => !cg.IsSpecial && cg.IsActive).Select(cg => new CharacterGroupLinkViewModel(cg)),
          IsAcceptingClaims = project.IsAcceptingClaims,
          IsActive = project.Active,
          CurrentUserId = CurrentUserId,
          RootGroupId = project.RootGroup.CharacterGroupId,
          HasAllrpg = project.Details?.AllrpgId != null
        };
      }
      else
      {
        ViewBag.PlayerMenu = new PlayerMenuViewModel
        {
          ProjectId = project.ProjectId,
          ProjectName = project.ProjectName,
          Claims = project.Claims.OfUserActive(CurrentUserIdOrDefault).Select(c => new ClaimShortListItemViewModel(c)).ToArray(),
          BigGroups = project.RootGroup.ChildGroups.Where(cg => !cg.IsSpecial && cg.IsActive).Select(cg => new CharacterGroupLinkViewModel(cg)),
          IsAcceptingClaims = project.IsAcceptingClaims,
          IsActive = project.Active,
          CurrentUserId = CurrentUserIdOrDefault,
          RootGroupId = project.RootGroup.IsAvailable ? (int?) project.RootGroup.CharacterGroupId : null
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
      if (!claim.HasAnyAccess(CurrentUserId))
      {
        return NoAccesToProjectView(claim.Project);
      }

      return WithEntity(claim);
    }

    protected ActionResult WithCharacter(Character character)
    {
      if (character == null)
      {
        return HttpNotFound();
      }
      if (!character.HasAnyAccess(CurrentUserId))
      {
        return NoAccesToProjectView(character.Project);
      }

      return WithEntity(character);
    }

    protected IDictionary<int,string> GetCustomFieldValuesFromPost()
    {
      return GetDynamicValuesFromPost(FieldValueViewModel.HtmlIdPrefix);
    }

    protected ActionResult AsMaster<TEntity>(TEntity entity) where TEntity : IProjectEntity
    {
      return AsMaster(entity, acl => true);
    }

    protected async Task<ActionResult> AsMaster<TEntity>(ICollection<TEntity> entity, int projectId) where TEntity : IProjectEntity
    {
      return AsMaster(entity.FirstOrDefault()?.Project ?? await ProjectRepository.GetProjectAsync(projectId), acl => true);
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
      return RedirectToIndex(project.ProjectId, project.RootGroup.CharacterGroupId);
    }

    protected ActionResult RedirectToIndex(int projectId, int characterGroupId, [AspMvcAction] string action = "Index")
    {
      return RedirectToAction(action, "GameGroups", new {projectId, characterGroupId, area = ""});
    }

    protected async Task<ActionResult> RedirectToProject(int projectId, [AspMvcAction] string action = "Index")
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      var errorResult = WithEntity(project);
      return errorResult ?? RedirectToIndex(project.ProjectId, project.RootGroup.CharacterGroupId, action);
    }

    protected static ExportType? GetExportTypeByName(string export)
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
      ExportDataService.BindDisplay<User>(user => user?.DisplayName);
      var generator = ExportDataService.GetGenerator(exportType, @select);
      return File(await generator.Generate(), generator.ContentType, Path.ChangeExtension(fileName, generator.FileExtension));
    }

    protected async Task<ActionResult> ExportWithCustomFronend<T>(IEnumerable<T> viewModel, string title, ExportType exportType, IGeneratorFrontend frontend, string projectName)
    {
      var generator = ExportDataService.GetGenerator(exportType, viewModel,
        frontend);

      return File(await generator.Generate(), generator.ContentType,
        Path.ChangeExtension(projectName + ": " + title, generator.FileExtension));
    }
  }
}
