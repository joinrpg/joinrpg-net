﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Web;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  [Authorize]
  public class MassMailController : Common.ControllerGameBase
  {
    private IClaimsRepository ClaimRepository { get; }
    private IEmailService EmailService { get; }

    [HttpGet]
    public async Task<ActionResult> ForClaims(int projectid, string claimIds)
    {
      var claims = (await ClaimRepository.GetClaimsByIds(projectid, claimIds.UnCompressIdList())).ToList();
      var project = claims.Select(c => c.Project).FirstOrDefault() ?? await ProjectRepository.GetProjectAsync(projectid);
      var canSendMassEmails = project.HasMasterAccess(CurrentUserId, acl => acl.CanSendMassMails);
      var filteredClaims = claims.Where(c => c.ResponsibleMasterUserId == CurrentUserId || canSendMassEmails).ToArray();
      return AsMaster(project) ?? View(new MassMailViewModel
      {
        AlsoMailToMasters = !claimIds.Any(),
        ProjectId = projectid,
        ProjectName = project.ProjectName,
        ClaimIds = filteredClaims.Select( c=> c.ClaimId).CompressIdList(),
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
      var canSendMassEmails = project.HasMasterAccess(CurrentUserId, acl => acl.CanSendMassMails);
      var filteredClaims = claims.Where(claim => claim.ResponsibleMasterUserId == CurrentUserId || canSendMassEmails).ToArray();
      var error = AsMaster(project);
      if (error != null)
      {
        return error;
      }
      try
      {
        
        var recepients =
          filteredClaims
            .Select(c => c.Player)
            .UnionIf(project.ProjectAcls.Select(acl => acl.User), viewModel.AlsoMailToMasters)
            .Distinct();

        await EmailService.Email(new MassEmailModel()
        {
          Initiator = await GetCurrentUserAsync(),
          ProjectName = project.ProjectName,
          Text = new MarkdownString(viewModel.Body),
          Recepients = recepients.ToList(),
          Subject = viewModel.Subject
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
    public MassMailController(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService, IExportDataService exportDataService, IClaimsRepository claimRepository, IEmailService emailService) : base(userManager, projectRepository, projectService, exportDataService)
    {
      ClaimRepository = claimRepository;
      EmailService = emailService;
    }

    #endregion
  }
}