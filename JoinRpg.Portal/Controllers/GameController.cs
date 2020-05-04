using System;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers
{
    public class GameController : Common.ControllerGameBase
    {
        public GameController(IProjectService projectService,
            IProjectRepository projectRepository,
            IUserRepository userRepository)
            : base(projectRepository, projectService, userRepository)
        {
        }

        [HttpGet("{projectId}/home")]
        //TODO enable this route w/o breaking everything [HttpGet("/{projectId:int}")]
        public async Task<IActionResult> Details(int projectId)
        {
            var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            return View(new ProjectDetailsViewModel(project));
        }

        [Authorize]
        [HttpGet("/game/create")]
        public IActionResult Create() => View(new ProjectCreateViewModel());

        // POST: Game/Create
        [Authorize]
        [HttpPost("/game/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var project = await ProjectService.AddProject(new CreateProjectRequest()
                {
                    ProjectType = (ProjectTypeDto)model.ProjectType,
                    ProjectName = model.ProjectName
                });

                return RedirectTo(project);
            }
            catch (Exception exception)
            {
                ModelState.AddException(exception);
                return View(model);
            }
        }

        private IActionResult RedirectTo(Project project) => RedirectToAction("Details", new { project.ProjectId });

        [HttpGet("/{projectId}/project/settings")]
        [MasterAuthorize(Permission.CanChangeProjectProperties)]
        public async Task<IActionResult> Edit(int projectId)
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
                EnableAccomodation = project.Details.EnableAccommodation,
            });
        }

        [HttpPost("/{projectId}/project/settings")]
        [MasterAuthorize(Permission.CanChangeProjectProperties), ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProjectViewModel viewModel)
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

        [HttpGet("/{projectId}/close")]
        [RequireMasterOrAdmin(Permission.CanChangeProjectProperties)]
        public async Task<IActionResult> Close(int projectid)
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

        [HttpPost("/{projectId}/close")]
        [RequireMasterOrAdmin(Permission.CanChangeProjectProperties)]
        public async Task<IActionResult> Close(CloseProjectViewModel viewModel)
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
