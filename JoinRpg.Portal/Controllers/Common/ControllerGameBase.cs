using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.Exporters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JoinRpg.Portal.Controllers.Common
{
    [JoinRpgExceptionHandler]
    [DiscoverProjectFilter]
    public abstract class ControllerGameBase : LegacyJoinControllerBase
    {
        [ProvidesContext, NotNull]
        protected IProjectService ProjectService { get; }
        public IProjectRepository ProjectRepository { get; }

        protected ControllerGameBase(
            IProjectRepository projectRepository,
            IProjectService projectService,
            IUserRepository userRepository
            ): base(userRepository)
        {
            if (projectRepository == null) throw new ArgumentNullException(nameof(projectRepository));
            ProjectRepository = projectRepository;
            ProjectService = projectService;
        }

        protected ActionResult NoAccesToProjectView(Project project)
        {
            return View("ErrorNoAccessToProject", new ErrorNoAccessToProjectViewModel(project));
        }

        //protected IReadOnlyDictionary<int, string> GetCustomFieldValuesFromPost() =>
        //    GetDynamicValuesFromPost(FieldValueViewModel.HtmlIdPrefix);

        [Obsolete]
        protected async Task<Project> GetProjectFromList(int projectId, IEnumerable<IProjectEntity> folders)
        {
            return folders.FirstOrDefault()?.Project ?? await ProjectRepository.GetProjectAsync(projectId);
        }


        protected ActionResult RedirectToIndex(Project project)
        {
            return RedirectToAction("Index", "GameGroups", new { project.ProjectId, area = "" });
        }

        protected ActionResult RedirectToIndex(int projectId, int characterGroupId, [AspMvcAction] string action = "Index")
        {
            return RedirectToAction(action, "GameGroups", new { projectId, characterGroupId, area = "" });
        }

        protected async Task<ActionResult> RedirectToProject(int projectId)
        {
            var project = await ProjectRepository.GetProjectAsync(projectId);
            return project == null ? NotFound() : RedirectToIndex(project);
        }

        [Obsolete]
        protected static ExportType? GetExportTypeByName(string export)
        {
            return ExportTypeNameParserHelper.ToExportType(export);
        }


        //protected async Task<FileContentResult> ReturnExportResult(string fileName, IExportGenerator generator)
        //{
        //    return File(await generator.Generate(), generator.ContentType,
        //      Path.ChangeExtension(fileName.ToSafeFileName(), generator.FileExtension));
        //}
    }
}
