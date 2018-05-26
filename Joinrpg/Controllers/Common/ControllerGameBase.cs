using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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
    [ProvidesContext, NotNull]
    protected IProjectService ProjectService { get; }
    [ProvidesContext, NotNull]
    private IExportDataService ExportDataService { get; }
    [ProvidesContext, NotNull]
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
      
      MenuViewModelBase menuModel;
      if (acl != null)
      {
        menuModel = new MasterMenuViewModel()
        {
          AccessToProject = acl,
          CheckInModuleEnabled = project.Details.EnableCheckInModule,
        };
      }
      else
      {
        menuModel = new PlayerMenuViewModel()
        {
          Claims = project.Claims.OfUserActive(CurrentUserIdOrDefault).Select(c => new ClaimShortListItemViewModel(c)).ToArray(),
          PlotPublished = project.Details.PublishPlot,
        };
      }
      menuModel.ProjectId = project.ProjectId;
      menuModel.ProjectName = project.ProjectName;
      //TODO[GroupsLoad]. If we not loaded groups already, that's slow
      menuModel.BigGroups = project.RootGroup.ChildGroups.Where(
          cg => !cg.IsSpecial && cg.IsActive && cg.IsVisible(CurrentUserIdOrDefault))
        .Select(cg => new CharacterGroupLinkViewModel(cg)).ToList();
      menuModel.IsAcceptingClaims = project.IsAcceptingClaims;
      menuModel.IsActive = project.Active;
      menuModel.RootGroupId = project.RootGroup.CharacterGroupId;
      menuModel.EnableAccommodation = project.Details.EnableAccommodation;
      menuModel.IsAdmin = IsCurrentUserAdmin();


      if (acl != null)
      {
        ViewBag.MasterMenu = menuModel;
      }
      else
      {
        ViewBag.PlayerMenu = menuModel;
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

      protected IReadOnlyDictionary<int, string> GetCustomFieldValuesFromPost() =>
          GetDynamicValuesFromPost(FieldValueViewModel.HtmlIdPrefix);

      protected ActionResult AsMaster<TEntity>(TEntity entity) where TEntity : IProjectEntity
    {
      return AsMaster(entity, acl => true);
    }

    [CanBeNull]
    protected ActionResult AsMaster<TEntity>(TEntity entity, Expression<Func<ProjectAcl, bool>> requiredRights) where TEntity : IProjectEntity
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

    [Obsolete]
    protected Task<FileContentResult> Export<T>(IEnumerable<T> select, string fileName, ExportType exportType = ExportType.Csv)
    {
      ExportDataService.BindDisplay<User>(user => user?.GetDisplayName());
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
