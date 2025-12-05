using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
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

namespace JoinRpg.Portal.Controllers;

public class GameController(
    IProjectService projectService,
    IProjectRepository projectRepository,
    IProjectMetadataRepository projectMetadataRepository
    ) : Common.ControllerGameBase(projectRepository, projectService)
{
    [HttpGet("{projectId}/home")]
    [AllowAnonymous]
    //TODO enable this route w/o breaking everything [HttpGet("/{projectId:int}")]
    public async Task<IActionResult> Details(ProjectIdentification projectId,
        [FromServices] IClaimsRepository claimsRepository,
        [FromServices] ICurrentUserAccessor currentUserAccessor,
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
            return View(new ProjectDetailsViewModel(project, details.ProjectDescription.ToHtmlString(), claims.ToClaimViewModels(), list, captainAccess));
        }
        else
        {
            return View(new ProjectDetailsViewModel(project, details.ProjectDescription.ToHtmlString(), [], list, []));
        }


    }

    [Authorize]
    [HttpGet("/game/create")]
    public IActionResult Create() => View(new ProjectCreateViewModel());

    private RedirectToActionResult RedirectTo(ProjectIdentification project) => RedirectToAction("Details", new { ProjectId = project.Value });

    [HttpGet("/{projectId}/project/settings")]
    [MasterAuthorize(Permission.CanChangeProjectProperties)]
    public async Task<IActionResult> Edit(int projectId)
    {
        var project = await ProjectRepository.GetProjectAsync(projectId);
        return View(new EditProjectViewModel
        {
            ClaimApplyRules = project.Details.ClaimApplyRules?.Contents ?? "",
            ProjectAnnounce = project.Details.ProjectAnnounce?.Contents ?? "",
            ProjectId = project.ProjectId,
            ProjectName = project.ProjectName,
            OriginalName = project.ProjectName,
            IsAcceptingClaims = project.IsAcceptingClaims,
            StrictlyOneCharacter = !project.Details.EnableManyCharacters,
            Active = project.Active,
            AutoAcceptClaims = project.Details.AutoAcceptClaims,
            EnableAccomodation = project.Details.EnableAccommodation,
            DefaultTemplateCharacterId = CharacterIdentification.FromOptional(project.ProjectId, project.Details.DefaultTemplateCharacterId),
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
                    ProjectId = new(viewModel.ProjectId),
                    ClaimApplyRules = viewModel.ClaimApplyRules,
                    IsAcceptingClaims = viewModel.IsAcceptingClaims,
                    MultipleCharacters = !viewModel.StrictlyOneCharacter,
                    ProjectAnnounce = viewModel.ProjectAnnounce,
                    ProjectName = viewModel.ProjectName,
                    AutoAcceptClaims = viewModel.AutoAcceptClaims,
                    IsAccommodationEnabled = viewModel.EnableAccomodation,
                    DefaultTemplateCharacterId = CharacterIdentification.FromOptional(viewModel.ProjectId, viewModel.DefaultTemplateCharacterIdInt),
                });

            return RedirectTo(new(project.ProjectId));
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
            project.HasMasterAccess(CurrentUserId, Permission.CanChangeProjectProperties);
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
            ProjectIdentification projectId = new(viewModel.ProjectId);
            await ProjectService.CloseProject(projectId, viewModel.PublishPlot);
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
