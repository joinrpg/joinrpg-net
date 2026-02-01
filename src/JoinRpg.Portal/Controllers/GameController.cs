using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.AdminTools.KogdaIgra;
using JoinRpg.Web.Games.Projects;
using JoinRpg.Web.Models;
using JoinRpg.Web.ProjectCommon.Projects;
using JoinRpg.WebPortal.Managers.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;

namespace JoinRpg.Portal.Controllers;

public class GameController(
    IProjectService projectService,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor
    ) : JoinControllerGameBase
{
    [HttpGet("{projectId}/home")]
    [AllowAnonymous]
    //TODO enable this route w/o breaking everything [HttpGet("/{projectId:int}")]
    public async Task<IActionResult> Details(ProjectIdentification projectId,
        [FromServices] IClaimsRepository claimsRepository,
        [FromServices] IKogdaIgraSyncClient kiClient,
        [FromServices] ICaptainRulesRepository captainRulesRepository)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        var details = await projectMetadataRepository.GetProjectDetails(projectId);

        if (project == null)
        {
            return NotFound();
        }

        var list = await kiClient.GetKogdaIgraCards(details.KogdaIgraLinkedIds);

        if (currentUserAccessor.UserIdentificationOrDefault is UserIdentification userId)
        {
            var claims = await claimsRepository.GetClaimsHeadersForPlayer(projectId, ClaimStatusSpec.ActiveOrOnHold, userId);
            var captainAccess = await captainRulesRepository.GetCaptainRules(projectId, userId);
            return View(new ProjectDetailsViewModel(project, details.ProjectDescription.ToHtmlString(), claims.ToClaimViewModels(), list, details.DisableKogdaIgraMapping, captainAccess));
        }
        else
        {
            return View(new ProjectDetailsViewModel(project, details.ProjectDescription.ToHtmlString(), [], list, details.DisableKogdaIgraMapping, []));
        }


    }

    [Authorize]
    [HttpGet("/game/create")]
    public IActionResult Create() => View(new ProjectCreateViewModel());

    private RedirectToActionResult RedirectTo(ProjectIdentification project) => RedirectToAction("Details", new { ProjectId = project.Value });

    [HttpGet("/{projectId}/project/settings")]
    [MasterAuthorize(Permission.CanChangeProjectProperties)]
    public async Task<IActionResult> Edit(int projectId, [FromServices] IProjectRepository projectRepository)
    {
        var project = await projectRepository.GetProjectAsync(projectId);
        return View(new EditProjectViewModel
        {
            ClaimApplyRules = project.Details.ClaimApplyRules?.Contents ?? "",
            ProjectAnnounce = project.Details.ProjectAnnounce?.Contents ?? "",
            ProjectId = project.ProjectId,
            ProjectName = project.ProjectName,
            OriginalName = project.ProjectName,
            Active = project.Active,
            EnableAccomodation = project.Details.EnableAccommodation,
        });
    }

    [HttpPost("/{projectId}/project/settings")]
    [MasterAuthorize(Permission.CanChangeProjectProperties), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProjectViewModel viewModel)
    {
        ProjectIdentification projectId = new(viewModel.ProjectId);

        try
        {
            await
                projectService.EditProject(new EditProjectRequest
                {
                    ProjectId = projectId,
                    ClaimApplyRules = viewModel.ClaimApplyRules,
                    ProjectAnnounce = viewModel.ProjectAnnounce,
                    ProjectName = viewModel.ProjectName,
                });

            await projectService.SetAccommodationSettings(projectId, viewModel.EnableAccomodation);

            return RedirectTo(projectId);
        }
        catch
        {
            var project = await projectMetadataRepository.GetProjectMetadata(projectId);
            viewModel.OriginalName = project.ProjectName;
            return View(viewModel);
        }
    }

    [HttpGet("/{projectId}/close")]
    [RequireMasterOrAdmin(Permission.CanChangeProjectProperties)]
    public async Task<IActionResult> Close(ProjectIdentification projectid)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectid);
        var isMaster = project.HasMasterAccess(currentUserAccessor, Permission.CanChangeProjectProperties);
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

        ProjectIdentification projectId = new(viewModel.ProjectId);

        try
        {
            await projectService.CloseProject(projectId, viewModel.PublishPlot);
            return RedirectToAction("Details", new { viewModel.ProjectId });
        }
        catch (Exception ex)
        {
            ModelState.AddException(ex);
            var project = await projectMetadataRepository.GetProjectMetadata(projectId);
            viewModel.OriginalName = project.ProjectName;
            viewModel.IsMaster = project.HasMasterAccess(currentUserAccessor, Permission.CanChangeProjectProperties);
            return View(viewModel);
        }
    }
}
