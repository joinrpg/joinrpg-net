using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CommonTypes;

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
      var claims = (await ClaimRepository.GetClaimsByIds(projectid, ToIntCollection(claimIds))).ToList();
      var project = claims.Select(c => c.Project).FirstOrDefault() ?? await ProjectRepository.GetProjectAsync(projectid);
      var canSendMassEmails = project.HasMasterAccess(CurrentUserId, acl => acl.CanSendMassMails);
      return AsMaster(project) ?? View(new MassMailViewModel()
      {
        AlsoMailToMasters = !claimIds.Any(),
        ProjectId = projectid,
        ProjectName = project.ProjectName,
        ClaimIds = claimIds,
        Claims = claims.Where(c => c.ResponsibleMasterUserId == CurrentUserId || canSendMassEmails).Select(claim => new ClaimShortListItemViewModel()
        {
          ClaimId = claim.ClaimId,
          Name = claim.Name,
          Player = claim.Player,
          ProjectId = claim.ProjectId
        }),
        ToMyClaimsOnlyWarning = !canSendMassEmails && claims.Any(c => c.ResponsibleMasterUserId != CurrentUserId),
        Body = new MarkdownViewModel("Добрый день, %NAME%, \nспешим уведомить вас..")
      });
    }

    private static int[] ToIntCollection(string claimIds)
    {
      return claimIds.Split(',').Select(ConvertIntRange).ToArray();
    }

    private static int ConvertIntRange(string c)
    {
      return int.Parse(c);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> ForClaims(MassMailViewModel viewModel)
    {
      var claims = (await ClaimRepository.GetClaimsByIds(viewModel.ProjectId, ToIntCollection(viewModel.ClaimIds))).ToList();
      var project = claims.Select(c => c.Project).FirstOrDefault() ?? await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var canSendMassEmails = project.HasMasterAccess(CurrentUserId, acl => acl.CanSendMassMails);
      var error = AsMaster(project);
      if (error != null)
      {
        return error;
      }
      try
      {
        var recepients =
          claims.Where(claim => claim.ResponsibleMasterUserId == CurrentUserId || canSendMassEmails)
            .Select(c => c.Player)
            .UnionIf(project.ProjectAcls.Select(acl => acl.User), viewModel.AlsoMailToMasters);

        await EmailService.Email(new MassEmailModel()
        {
          Initiator = await GetCurrentUserAsync(),
          ProjectName = project.ProjectName,
          Text = viewModel.Body,
          Recepients = recepients.ToList(),
          Subject = viewModel.Subject
        });
        return View("Success");
      }
      catch (Exception)
      {
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