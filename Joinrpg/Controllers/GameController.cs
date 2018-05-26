using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
    public class GameController : Common.ControllerGameBase
    {
        public GameController(IProjectService projectService,
            ApplicationUserManager userManager,
            IProjectRepository projectRepository,
            IExportDataService exportDataService)
            : base(userManager, projectRepository, projectService, exportDataService)
        {
        }

        public async Task<ActionResult> Details(int projectId)
        {
            var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId);
            if (project == null) return HttpNotFound();
            return View(new ProjectDetailsViewModel(project));
        }

        [Authorize]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Game/Create
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ProjectCreateViewModel model)
        {
            try
            {
                var project = await ProjectService.AddProject(model.ProjectName);

                return RedirectTo(project);
            }
            catch (Exception exception)
            {
                ModelState.AddException(exception);
                return View(model);
            }
        }

        private ActionResult RedirectTo(Project project)
        {
            return RedirectToAction("Details", new {project.ProjectId});
        }

        [HttpGet, MasterAuthorize(Permission.CanChangeProjectProperties)]
        public async Task<ActionResult> Edit(int projectId)
        {
            var project = await ProjectRepository.GetProjectAsync(projectId);
            return View(new EditProjectViewModel
            {
                ClaimApplyRules = project.Details.ClaimApplyRules.Contents,
                ProjectAnnounce = project.Details.ProjectAnnounce.Contents,
                ProjectId = project.ProjectId,
                ProjectName = project.ProjectName,
                OriginalName = project.ProjectName,
                IsAcceptingClaims = project.IsAcceptingClaims,
                PublishPlot = project.Details.PublishPlot,
                StrictlyOneCharacter = !project.Details.EnableManyCharacters,
                Active = project.Active,
                AutoAcceptClaims = project.Details.AutoAcceptClaims,
                GenerateCharacterNamesFromPlayer = project.Details.GenerateCharacterNamesFromPlayer,
                EnableAccomodation = project.Details.EnableAccommodation,
            });
        }

        [HttpPost, MasterAuthorize(Permission.CanChangeProjectProperties), ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(EditProjectViewModel viewModel)
        {
            var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
            try
            {
                await
                    ProjectService.EditProject(new EditProjectRequest
                    {
                        ProjectId = viewModel.ProjectId,
                        ClaimApplyRules = viewModel.ClaimApplyRules,
                        IsAcceptingClaims = viewModel.IsAcceptingClaims,
                        MultipleCharacters = !viewModel.StrictlyOneCharacter,
                        ProjectAnnounce = viewModel.ProjectAnnounce,
                        ProjectName = viewModel.ProjectName,
                        PublishPlot = viewModel.PublishPlot,
                        AutoAcceptClaims = viewModel.AutoAcceptClaims,
                        GenerateCharacterNamesFromPlayer = viewModel.GenerateCharacterNamesFromPlayer,
                        IsAccommodationEnabled = viewModel.EnableAccomodation,
                    });

                return RedirectTo(project);
            }
            catch
            {
                viewModel.OriginalName = project.ProjectName;
                return View(viewModel);
            }
        }

        [HttpGet,
         MasterAuthorize(Permission = Permission.CanChangeProjectProperties, AllowAdmin = true)]
        public async Task<ActionResult> Close(int projectid)
        {
            var project = await ProjectRepository.GetProjectAsync(projectid);
            var isMaster =
                project.HasMasterAccess(CurrentUserId, acl => acl.CanChangeProjectProperties);
            return View(new CloseProjectViewModel()
            {
                OriginalName = project.ProjectName,
                ProjectId = projectid,
                PublishPlot = isMaster,
                IsMaster = isMaster,
            });
        }

        [HttpPost,
         MasterAuthorize(AllowAdmin = true, Permission = Permission.CanChangeProjectProperties)]
        public async Task<ActionResult> Close(CloseProjectViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            try
            {
                await ProjectService.CloseProject(viewModel.ProjectId,
                    CurrentUserId,
                    viewModel.PublishPlot);
                return await RedirectToProject(viewModel.ProjectId);
            }
            catch (Exception ex)
            {
                ModelState.AddException(ex);
                var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
                viewModel.OriginalName = project.ProjectName;
                viewModel.IsMaster =
                    project.HasMasterAccess(CurrentUserId, acl => acl.CanChangeProjectProperties);
                return View(viewModel);
            }
        }
    }
}
