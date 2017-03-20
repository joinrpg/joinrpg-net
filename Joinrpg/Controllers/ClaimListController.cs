﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Exporters;
using JoinRpg.Helpers;
using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Controllers
{
  public class ClaimListController : Common.ControllerGameBase
  {
    private IClaimsRepository ClaimsRepository { get; }
    private IUriService UriService { get; }

    public ClaimListController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IExportDataService exportDataService, IClaimsRepository claimsRepository,
      IUriService uriService)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
      ClaimsRepository = claimsRepository;
      UriService = uriService;
    }

    #region implementation

    private async Task<ActionResult> ShowMasterClaimList(int projectId, string export, string title,
      [AspMvcView] string viewName, IReadOnlyCollection<Claim> claims)
    {
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

        return
          await
            ExportWithCustomFronend(view.Items, title, exportType.Value,
              new ClaimListItemViewModelExporter(project.ProjectFields, UriService), project.ProjectName);
      }
    }

    private async Task<ActionResult> ShowMasterClaimListForGroup(CharacterGroup characterGroup, string export, string title, IReadOnlyCollection<Claim> claims, GroupNavigationPage page)
    {
      var error = AsMaster(characterGroup);
      if (error != null) return error;

      var view = new ClaimListForGroupViewModel(CurrentUserId, claims, characterGroup, page);

      var exportType = GetExportTypeByName(export);

      if (exportType == null)
      {
        ViewBag.MasterAccessColumn = true;
        ViewBag.Title = title + " " + characterGroup.CharacterGroupName;
        return View("ByGroup", view);
      }
      else
      {
        return
          await
            ExportWithCustomFronend(view.Items, title, exportType.Value,
              new ClaimListItemViewModelExporter(characterGroup.Project.ProjectFields, UriService), characterGroup.Project.ProjectName);
      }
    }


    #endregion

    [HttpGet, Authorize]
    public async Task<ActionResult> ForPlayer(int projectId, int userId, string export)
    {
      var claims = await ClaimsRepository.GetClaimsForPlayer(projectId, ClaimStatusSpec.Active, userId);

      return await ShowMasterClaimList(projectId, export, "Заявки на игроке", "Index", claims);
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> ListForGroupDirect(int projectId, int characterGroupId, string export)
    {
      var group = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
      var claims = await ClaimsRepository.GetClaimsForGroupDirect(projectId, ClaimStatusSpec.Active, characterGroupId);

      return await ShowMasterClaimListForGroup(group, export, "Обсуждаемые заявки", claims, GroupNavigationPage.ClaimsDirect);
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> ListForGroup(int projectId, int characterGroupId, string export)
    {
      var group = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
      var groupIds = await GetChildrenGroupIds(projectId, characterGroupId);
      var claims = await ClaimsRepository.GetClaimsForGroups(projectId, ClaimStatusSpec.Active, groupIds);

      return await ShowMasterClaimListForGroup(group, export, "Заявки в группу (все)", claims, GroupNavigationPage.ClaimsActive);
    }

    private async Task<int[]> GetChildrenGroupIds(int projectId, int characterGroupId)
    {
      var groups = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
      return groups.GetChildrenGroups().Select(g => g.CharacterGroupId).Union(characterGroupId).ToArray();
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> DiscussingForGroup(int projectId, int characterGroupId, string export)
    {
      var group = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
      var groupIds = await GetChildrenGroupIds(projectId, characterGroupId);
      var claims = await ClaimsRepository.GetClaimsForGroups(projectId, ClaimStatusSpec.Discussion, groupIds);

      return await ShowMasterClaimListForGroup(group, export, "Обсуждаемые заявки в группу (все)",
        claims, GroupNavigationPage.ClaimsDiscussing);
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> ResponsibleDiscussing(int projectid, int responsibleMasterId, string export)
    {
      var claims = await ClaimsRepository.GetActiveClaimsForMaster(projectid, responsibleMasterId,
        ClaimStatusSpec.Discussion);

      return await ShowMasterClaimList(projectid, export, "Обсуждаемые заявки на мастере", "Index", claims);
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> ResponsibleOnHold(int projectid, int responsiblemasterid, string export)
    {
      var claims = await ClaimsRepository.GetActiveClaimsForMaster(projectid, responsiblemasterid,
        ClaimStatusSpec.OnHold);

      return await ShowMasterClaimList(projectid, export, "Лист ожидания на мастере", "Index", claims);
    }


    [HttpGet, Authorize]
    public async Task<ActionResult> ActiveList(int projectId, string export)
    {
      var claims = await ClaimsRepository.GetClaims(projectId, ClaimStatusSpec.Active);

      return await ShowMasterClaimList(projectId, export, "Активные заявки", "Index", claims);
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> DeclinedList(int projectId, string export)
    {
      var claims = (await ClaimsRepository.GetClaims(projectId, ClaimStatusSpec.InActive)).ToList();

      return await ShowMasterClaimList(projectId, export, "Отклоненные/отозванные заявки", "Index", claims);
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> Discussing(int projectId, string export)
    {
      var claims = (await ClaimsRepository.GetClaims(projectId, ClaimStatusSpec.Discussion)).ToList();

      return await ShowMasterClaimList(projectId, export, "Обсуждаемые заявки", "Index", claims);
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> OnHoldList(int projectid, string export)
    {
      var claims = (await ClaimsRepository.GetClaims(projectid, ClaimStatusSpec.OnHold)).ToList();

      return await ShowMasterClaimList(projectid, export, "Лист ожидания", "Index", claims);
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> WaitingForFee(int projectid, string export)
    {
      var claims =
        (await ClaimsRepository.GetClaims(projectid, ClaimStatusSpec.Approved)).Where(claim => !claim.ClaimPaidInFull())
        .ToList();

      return await ShowMasterClaimList(projectid, export, "Неоплаченные принятые заявки", "Index", claims);
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> SomeFieldsToFill(int projectid, string export)
    {
      var project = await ProjectRepository.GetProjectAsync(projectid);
      var claims =
        (await ClaimsRepository.GetClaims(projectid, ClaimStatusSpec.Approved)).Where(
          claim => claim.HasProblemsForFields(project.ProjectFields.Where(p => p.CanPlayerEdit))).ToList();

      return await ShowMasterClaimList(projectid, export, "Заявки с незаполненными полями", "Index", claims);
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> Responsible(int projectid, int responsibleMasterId, string export)
    {
      var claims =
        (await ClaimsRepository.GetActiveClaimsForMaster(projectid, responsibleMasterId, ClaimStatusSpec.Active)).ToList
        ();

      return await ShowMasterClaimList(projectid, export, "Заявки на мастере", "Index", claims);
    }

    [HttpGet, Authorize]
    public ActionResult My()
    {
      ViewBag.Title = "Мои заявки";
      return View("Index",
        new ClaimListViewModel(CurrentUserId, GetCurrentUser().Claims.ToList(), null, showCount: false,
          showUserColumn: false));
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> Problems(int projectId, string export)
    {
      var claims =
        (await ClaimsRepository.GetClaims(projectId, ClaimStatusSpec.Any)).Where(
          c => c.GetProblems().Any(p => p.Severity >= ProblemSeverity.Warning)).ToList();
      return
        await ShowMasterClaimList(projectId, export, "Проблемные заявки", "Index", claims);
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> ResponsibleProblems(int projectId, int responsibleMasterId, string export)
    {
      var claims =
        (await ClaimsRepository.GetActiveClaimsForMaster(projectId, responsibleMasterId, ClaimStatusSpec.Any)).Where(
          claim =>
            claim.GetProblems().Any(p => p.Severity >= ProblemSeverity.Warning)).ToList();

      return await ShowMasterClaimList(projectId, export, "Проблемные заявки на мастере", "Index", claims);
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> PaidDeclined(int projectid, string export)
    {
      var claims =
        (await ClaimsRepository.GetClaims(projectid, ClaimStatusSpec.InActive)).Where(claim => claim.ClaimBalance() > 0)
        .ToList();

      return await ShowMasterClaimList(projectid, export, "Оплаченные отклоненные заявки", "Index", claims);
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> ByAssignedField(int projectfieldid, int projectid, string export)
    {
      var field = await ProjectRepository.GetProjectField(projectid, projectfieldid);
      var claims = await ClaimsRepository.GetClaims(projectid, ClaimStatusSpec.Active);
      return await ShowMasterClaimList(projectid, export, "Поле (проставлено): " + field.FieldName, "Index",
        claims.Where(c => c.GetFields().Single(f => f.Field.ProjectFieldId == projectfieldid).HasValue).ToList()
        );
    }
  }
}
