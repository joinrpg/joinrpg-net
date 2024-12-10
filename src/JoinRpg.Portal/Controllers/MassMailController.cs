using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers.Web;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.WebPortal.Managers;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[MasterAuthorize()]
[Route("{projectId}/massmail/[action]")]
public class MassMailController(
    IProjectRepository projectRepository,
    IProjectService projectService,
    IClaimsRepository claimRepository,
    IUserRepository userRepository,
    MassMailManager massMailManager) : Common.ControllerGameBase(projectRepository, projectService, userRepository)
{
    [HttpGet]
    public async Task<ActionResult> ForClaims(int projectid, string claimIds)
    {
        var claims = (await claimRepository.GetClaimsByIds(projectid, claimIds.UnCompressIdList())).ToList();
        var project = claims.Select(c => c.Project).FirstOrDefault() ?? await ProjectRepository.GetProjectAsync(projectid);

        if (!project.Active)
        {
            return View("ErrorNotActiveProject");
        }
        var canSendMassEmails = project.HasMasterAccess(CurrentUserId, acl => acl.CanSendMassMails);
        var filteredClaims = claims.Where(c => c.ResponsibleMasterUserId == CurrentUserId || canSendMassEmails).ToArray();
        return View(new MassMailViewModel
        {
            AlsoMailToMasters = !claimIds.Any(),
            ProjectId = projectid,
            ProjectName = project.ProjectName,
            ClaimIds = filteredClaims.Select(c => c.ClaimId).CompressIdList(),
            Claims = filteredClaims.Select(claim => new ClaimShortListItemViewModel(claim)),
            ToMyClaimsOnlyWarning = !canSendMassEmails && claims.Any(c => c.ResponsibleMasterUserId != CurrentUserId),
            Body = "Добрый день, %NAME%, \nспешим уведомить вас..",
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> ForClaims(MassMailViewModel viewModel)
    {
        try
        {
            await massMailManager.MassMail(
                new(viewModel.ProjectId),
                [.. viewModel.ClaimIds.UnCompressIdList()],
                new MarkdownString(viewModel.Body),
                viewModel.Subject,
                viewModel.AlsoMailToMasters
                );
            return View("Success");
        }
        catch (Exception exception)
        {

            var claims = (await claimRepository.GetClaimsByIds(viewModel.ProjectId, viewModel.ClaimIds.UnCompressIdList())).ToList();
            var project = claims.Select(c => c.Project).FirstOrDefault() ?? await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
            var canSendMassEmails = project.HasMasterAccess(CurrentUserId, acl => acl.CanSendMassMails);
            viewModel.Claims = claims.Select(claim => new ClaimShortListItemViewModel(claim));
            viewModel.ToMyClaimsOnlyWarning = !canSendMassEmails &&
                                              claims.Any(c => c.ResponsibleMasterUserId != CurrentUserId);
            viewModel.ProjectName = project.ProjectName;
            ModelState.AddException(exception);
            ModelState.AddModelError("", "При отправке письма произошла ошибка");
            return View(viewModel);
        }
    }
}
