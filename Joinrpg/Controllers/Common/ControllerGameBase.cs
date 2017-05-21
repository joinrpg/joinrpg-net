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
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.Web.Controllers.Common
{
  [JoinRpgExceptionHandler]
  public class ControllerGameBase : ControllerBase
  {
    protected IProjectService ProjectService { get; }
    private IExportDataService ExportDataService { get; }
    [NotNull]
    public IProjectRepository ProjectRepository { get; }

    protected ControllerGameBase(ApplicationUserManager userManager, [NotNull] IProjectRepository projectRepository,
      IProjectService projectService, IExportDataService exportDataService) : base(userManager)
    {
      if (projectRepository == null) throw new ArgumentNullException(nameof(projectRepository));
      ProjectRepository = projectRepository;
      ProjectService = projectService;
      ExportDataService = exportDataService;
    }

    protected ActionResult NoAccesToProjectView(Project project)
    {
      return View("ErrorNoAccessToProject", new ErrorNoAccessToProjectViewModel(project));
    }


    protected override void OnActionExecuting(ActionExecutingContext filterContext)
    {
      var projectIdValue = ValueProvider.GetValue("projectid");
      if (projectIdValue == null)
      {
        return;
      }
      var projectIdRawValue = projectIdValue.RawValue;
      var projectId = projectIdRawValue.GetType().IsArray ? int.Parse(((string[])projectIdRawValue)[0]) : int.Parse((string)projectIdRawValue);

      var project = ProjectRepository.GetProjectAsync(projectId).Result;
      RegisterProjectMenu(project);

      base.OnActionExecuting(filterContext);
    }

    private void RegisterProjectMenu(Project project)
    {
      ViewBag.ProjectId = project.ProjectId;

      var acl = project.ProjectAcls.FirstOrDefault(a => a.UserId == CurrentUserIdOrDefault);
      //TODO[GroupsLoad]. If we not loaded groups already, that's slow
      var bigGroups = project.RootGroup.ChildGroups.Where(cg => !cg.IsSpecial && cg.IsActive);
      if (acl != null)
      {
        ViewBag.MasterMenu = new MasterMenuViewModel
        {
          ProjectId = project.ProjectId,
          ProjectName = project.ProjectName,
          AccessToProject = acl,
          BigGroups = bigGroups.Select(cg => new CharacterGroupLinkViewModel(cg)),
          IsAcceptingClaims = project.IsAcceptingClaims,
          IsActive = project.Active,
          RootGroupId = project.RootGroup.CharacterGroupId,
          IsAdmin = IsCurrentUserAdmin(),
        };
      }
      else
      {
        ViewBag.PlayerMenu = new PlayerMenuViewModel
        {
          ProjectId = project.ProjectId,
          ProjectName = project.ProjectName,
          Claims =
            project.Claims.OfUserActive(CurrentUserIdOrDefault).Select(c => new ClaimShortListItemViewModel(c)).ToArray(),
          BigGroups =
            bigGroups.Where(cg => cg.IsPublic || project.IsPlotPublished())
              .Select(cg => new CharacterGroupLinkViewModel(cg)),
          IsAcceptingClaims = project.IsAcceptingClaims,
          IsActive = project.Active,
          RootGroupId = project.RootGroup.IsAvailable ? (int?) project.RootGroup.CharacterGroupId : null,
          PlotPublished = project.Details.PublishPlot,
          IsAdmin = IsCurrentUserAdmin(),
        };
      }
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

      return null;
    }

    protected IDictionary<int,string> GetCustomFieldValuesFromPost()
    {
      return GetDynamicValuesFromPost(FieldValueViewModel.HtmlIdPrefix);
    }

    protected ActionResult AsMaster<TEntity>(TEntity entity) where TEntity : IProjectEntity
    {
      return AsMaster(entity, acl => true);
    }

    [CanBeNull]
    protected ActionResult AsMaster<TEntity>(TEntity entity, Func<ProjectAcl, bool> requiredRights) where TEntity : IProjectEntity
    {
      return entity == null ? HttpNotFound() :
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
      return project == null ? HttpNotFound() : RedirectToIndex(project.ProjectId, project.RootGroup.CharacterGroupId, action);
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

    protected Task<FileContentResult> Export<T>(IEnumerable<T> select, string fileName, ExportType exportType = ExportType.Csv)
    {
      ExportDataService.BindDisplay<User>(user => user?.DisplayName);
      var generator = ExportDataService.GetGenerator(exportType, select);
      return  ReturnExportResult(fileName, generator);
    }

    private async Task<FileContentResult> ReturnExportResult(string fileName, IExportGenerator generator)
    {
      return File(await generator.Generate(), generator.ContentType,
        Path.ChangeExtension(fileName.ToSafeFileName(), generator.FileExtension));
    }

    protected Task<FileContentResult> ExportWithCustomFronend<T>(IEnumerable<T> viewModel, string title,
      ExportType exportType, IGeneratorFrontend frontend, string projectName)
    {
      var generator = ExportDataService.GetGenerator(exportType, viewModel,
        frontend);

      return ReturnExportResult(projectName + ": " + title, generator);
    }
  }
}
