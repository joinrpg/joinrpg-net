using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Web;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces.Notification;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.ClaimList;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[MasterAuthorize()]
[Route("{projectId}/massmail/[action]")]
public class MassMailController : Common.ControllerGameBase
{
    private IClaimsRepository ClaimRepository { get; }
    private IEmailService EmailService { get; }

    [HttpGet]
    public async Task<ActionResult> ForClaims(int projectid, string claimIds)
    {
        var claims = (await ClaimRepository.GetClaimsByIds(projectid, claimIds.UnCompressIdList())).ToList();
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
        var claims = (await ClaimRepository.GetClaimsByIds(viewModel.ProjectId, viewModel.ClaimIds.UnCompressIdList())).ToList();
        var project = claims.Select(c => c.Project).FirstOrDefault() ?? await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
        _ = project.EnsureProjectActive();
        var canSendMassEmails = project.HasMasterAccess(CurrentUserId, acl => acl.CanSendMassMails);
        var filteredClaims = claims.Where(claim => claim.ResponsibleMasterUserId == CurrentUserId || canSendMassEmails).ToArray();

        try
        {

            var recipients =
              filteredClaims
                .Select(c => c.Player)
                .UnionIf(project.ProjectAcls.Select(acl => acl.User), viewModel.AlsoMailToMasters)
                .Distinct();

            await EmailService.Email(new MassEmailModel()
            {
                Initiator = await GetCurrentUserAsync(),
                ProjectName = project.ProjectName,
                Text = new MarkdownString(viewModel.Body),
                Recipients = recipients.ToList(),
                Subject = viewModel.Subject,
            });
            return View("Success");
        }
        catch (Exception exception)
        {
            viewModel.Claims = filteredClaims.Select(claim => new ClaimShortListItemViewModel(claim));
            viewModel.ToMyClaimsOnlyWarning = !canSendMassEmails &&
                                              claims.Any(c => c.ResponsibleMasterUserId != CurrentUserId);
            viewModel.ProjectName = project.ProjectName;
            ModelState.AddException(exception);
            ModelState.AddModelError("", "При отправке письма произошла ошибка");
            return View(viewModel);
        }
    }

    #region constructor
    public MassMailController(
        IProjectRepository projectRepository,
        IProjectService projectService,
        IClaimsRepository claimRepository,
        IEmailService emailService,
        IUserRepository userRepository) : base(projectRepository, projectService, userRepository)
    {
        ClaimRepository = claimRepository;
        EmailService = emailService;
    }

    #endregion
}
