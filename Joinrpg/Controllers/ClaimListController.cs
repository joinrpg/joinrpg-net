using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  public class ClaimListController : Common.ControllerGameBase
  {
    private IClaimsRepository ClaimsRepository { get; }

    public ClaimListController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IExportDataService exportDataService, IClaimsRepository claimsRepository)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
      ClaimsRepository = claimsRepository;
    }

    #region implementation
    private async Task<ActionResult> MasterClaimList(int projectId, Func<Claim, bool> predicate, string export,
      string title, [AspMvcView] string viewName = "Index")
    {
      var claims = (await ClaimsRepository.GetClaims(projectId)).Where(predicate).ToList();

      var error = await AsMaster(claims, projectId);
      if (error != null) return error;

      var view = new ClaimListViewModel(CurrentUserId, claims, projectId);

      var exportType = GetExportTypeByName(export);

      if (exportType == null)
      {
        ViewBag.MasterAccessColumn = true;
        ViewBag.Title = title;
        return View(viewName, view);
      }
      else
      {
        var project = await GetProjectFromList(projectId, claims);

        return await ExportWithCustomFronend(view.Items, title, exportType.Value, new ClaimListItemViewModelExporter(project.ProjectFields), project.ProjectName);
      }
    }
    #endregion

    [HttpGet, Authorize]
    public Task<ActionResult> ForPlayer(int projectId, int userId, string export)
      => MasterClaimList(projectId, cl => cl.IsActive & cl.PlayerUserId == userId, export, "Заявки на игроке");

    [HttpGet, Authorize]
    public Task<ActionResult> ListForGroupDirect(int projectId, int characterGroupId, string export)
    {
      ViewBag.CharacterGroupId = characterGroupId;
      return MasterClaimList(projectId, cl => cl.IsActive && cl.CharacterGroupId == characterGroupId, export,
        "Заявки в группу (напрямую)", "ListForGroupDirect");
    }

    [HttpGet, Authorize]
    public Task<ActionResult> ListForGroup(int projectId, int characterGroupId, string export)
    {
      ViewBag.CharacterGroupId = characterGroupId;
      return MasterClaimList(projectId, cl => cl.IsActive && cl.IsPartOfGroup(characterGroupId), export,
        "Заявки в группу (все)", "ListForGroup");
    }

    [HttpGet, Authorize]
    public Task<ActionResult> DiscussingForGroup(int projectId, int characterGroupId, string export)
    {
      ViewBag.CharacterGroupId = characterGroupId;
      return MasterClaimList(projectId, cl => cl.IsInDiscussion && cl.IsPartOfGroup(characterGroupId), export,
        "Обсуждаемые заявки в группу (все)", "DiscussingForGroup");
    }

    [HttpGet, Authorize]
    public Task<ActionResult> DiscussingForGroupDirect(int projectId, int characterGroupId, string export)
    {
      ViewBag.CharacterGroupId = characterGroupId;
      return MasterClaimList(projectId, cl => cl.IsInDiscussion && cl.CharacterGroupId == characterGroupId, export,
        "Обсуждаемые заявки в группу (напрямую)", "DiscussingForGroupDirect");
    }

    [HttpGet, Authorize]
    public Task<ActionResult> ResponsibleDiscussing(int projectid, int responsibleMasterId, string export)
      =>
        MasterClaimList(projectid, claim => claim.ResponsibleMasterUserId == responsibleMasterId && claim.IsInDiscussion,
          export, "Обсуждаемые заявки на мастере");

    [HttpGet, Authorize]
    public Task<ActionResult> ResponsibleOnHold(int projectid, int responsiblemasterid, string export)
      =>
        MasterClaimList(projectid,
          claim => claim.ResponsibleMasterUserId == responsiblemasterid && claim.ClaimStatus == Claim.Status.OnHold,
          export, "Лист ожидания на мастере");


    [HttpGet, Authorize]
    public Task<ActionResult> ActiveList(int projectid, string export)
      => MasterClaimList(projectid, claim => claim.IsActive, export, "Активные заявки");

    [HttpGet, Authorize]
    public Task<ActionResult> DeclinedList(int projectid, string export)
      => MasterClaimList(projectid, claim => !claim.IsActive, export, "Отклоненные/отозванные заявки");

    [HttpGet, Authorize]
    public Task<ActionResult> Discussing(int projectid, string export)
      => MasterClaimList(projectid, claim => claim.IsInDiscussion, export, "Обсуждаемые заявки");

    [HttpGet, Authorize]
    public Task<ActionResult> OnHoldList(int projectid, string export)
      => MasterClaimList(projectid, claim => claim.ClaimStatus == Claim.Status.OnHold, export, "Лист ожидания");

    [HttpGet, Authorize]
    public Task<ActionResult> WaitingForFee(int projectid, string export)
      =>
        MasterClaimList(projectid, claim => claim.IsApproved && !claim.ClaimPaidInFull(), export,
          "Неоплаченные принятые заявки");

    [HttpGet, Authorize]
    public Task<ActionResult> Responsible(int projectid, int responsibleMasterId, string export)
      =>
        MasterClaimList(projectid, claim => claim.ResponsibleMasterUserId == responsibleMasterId && claim.IsActive,
          export, "Заявки на мастере");

    [HttpGet, Authorize]
    public ActionResult My()
    {
      ViewBag.Title = "Мои заявки";
      ViewBag.HideUserColumn = true;
      return View("Index", new ClaimListViewModel(CurrentUserId, GetCurrentUser().Claims, null));
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> Problems(int projectId, string export)
    {
      return
        await
          MasterClaimList(projectId, c => c.GetProblems().Any(p => p.Severity >= ProblemSeverity.Warning), export,
            "Проблемные заявки");
    }

    [HttpGet, Authorize]
    public Task<ActionResult> ResponsibleProblems(int projectId, int responsibleMasterId, string export)
    {
      return
        MasterClaimList(projectId,
          claim =>
            claim.ResponsibleMasterUserId == responsibleMasterId &&
            claim.GetProblems().Any(p => p.Severity >= ProblemSeverity.Warning),
          export, "Проблемные заявки на мастере");
    }

   
  }
}