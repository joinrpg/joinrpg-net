using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.Helpers;
using JoinRpg.Portal.Helpers;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.Web.Models.Exporters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{ProjectId}/claims/[action]")]
public class ClaimListController(
    IProjectRepository projectRepository,
    IProjectService projectService,
    IExportDataService exportDataService,
    IClaimsRepository claimsRepository,
    IUriService uriService,
    IAccommodationRepository accommodationRepository,
    IUserRepository userRepository,
    IProblemValidator<Claim> claimValidator,
    IProjectMetadataRepository projectMetadataRepository
        ) : Common.ControllerGameBase(projectRepository, projectService, userRepository)
{

    #region implementation

    private async Task<ActionResult> ___ShowMasterClaimList(int projectId, string export, string title, IReadOnlyCollection<Claim> claims, ClaimStatusSpec claimStatusSpec)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));
        var exportType = ExportTypeNameParserHelper.ToExportType(export);

        if (exportType == null)
        {
            ViewBag.MasterAccessColumn = true;
            var unreadComments = await claimsRepository.GetUnreadDiscussionsForClaims(projectId, claimStatusSpec, CurrentUserId, hasMasterAccess: true);
            var view = new RegularClaimListViewModel(CurrentUserId, claims, projectId, unreadComments, claimValidator, projectInfo, title);
            return View("Index", view);
        }
        else
        {
            var view = new ClaimListForExportViewModel(CurrentUserId, claims, projectInfo);

            return
                    ExportWithCustomFrontend(view.Items, title, exportType.Value,
                        new ClaimListItemViewModelExporter(uriService, projectInfo), projectInfo.ProjectName);
        }
    }

    private async Task<ActionResult> ShowMasterClaimList(int projectId, string export, string title, IReadOnlyCollection<Claim> claims, ClaimStatusSpec claimStatusSpec)
    {

        return await ___ShowMasterClaimList(projectId, export, title, claims, claimStatusSpec);
    }

    private async Task<ActionResult> ShowMasterClaimList(int projectId, string export, string title, ClaimStatusSpec claimStatusSpec)
    {
        var claims = await claimsRepository.GetClaims(projectId, claimStatusSpec);

        return await ___ShowMasterClaimList(projectId, export, title, claims, claimStatusSpec);

    }

    private async Task<ActionResult> ShowMasterClaimList(int projectId, string export, string title, ClaimStatusSpec claimStatusSpec, int masterUserId)
    {
        var claims = await claimsRepository.GetClaimsForMaster(projectId, masterUserId, claimStatusSpec);

        return await ___ShowMasterClaimList(projectId, export, title, claims, claimStatusSpec);

    }

    private async Task<ActionResult> ShowMasterClaimList(int projectId, string export, string title, ClaimStatusSpec claimStatusSpec, int masterUserId, Func<Claim, ProjectInfo, bool> predicate)
    {
        var claims = await claimsRepository.GetClaimsForMaster(projectId, masterUserId, claimStatusSpec);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));

        return await ___ShowMasterClaimList(projectId, export, title, claims.Where(c => predicate(c, projectInfo)).ToList(), claimStatusSpec);

    }

    private async Task<ActionResult> ShowMasterClaimListForGroup(CharacterGroup characterGroup, string export,
        string title, IReadOnlyCollection<Claim> claims, GroupNavigationPage page, ClaimStatusSpec claimStatusSpec)
    {
        if (characterGroup == null)
        {
            return NotFound();
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(characterGroup.ProjectId));

        var exportType = ExportTypeNameParserHelper.ToExportType(export);

        if (exportType == null)
        {
            var unreadComments = await claimsRepository.GetUnreadDiscussionsForClaims(characterGroup.ProjectId, claimStatusSpec, CurrentUserId, hasMasterAccess: true);
            var view = new ClaimListForGroupViewModel(CurrentUserId, claims, characterGroup, page, unreadComments, claimValidator, projectInfo, title);
            ViewBag.MasterAccessColumn = true;
            return View("ByGroup", view);
        }
        else
        {
            var view = new ClaimListForExportViewModel(CurrentUserId, claims, projectInfo);
            return
                    ExportWithCustomFrontend(view.Items, title, exportType.Value,
                        new ClaimListItemViewModelExporter(uriService, projectInfo),
                        characterGroup.Project.ProjectName);
        }
    }

    #endregion

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> ForPlayer(int projectId, int userId, string export)
    {
        var claims = await claimsRepository.GetClaimsForPlayer(projectId, ClaimStatusSpec.Active, userId);

        return await ShowMasterClaimList(projectId, export, "Заявки на игроке", claims, ClaimStatusSpec.Active);
    }

    [HttpGet("~/{ProjectId}/claims/without-roomtype")]
    [MasterAuthorize()]
    public Task<ActionResult> ListWithoutRoomType(int projectId, string export) =>
        ListForRoomType(projectId, null, export);

    [HttpGet("~/{ProjectId}/claims/by-roomtype/{roomTypeId?}")]
    [MasterAuthorize()]
    public async Task<ActionResult> ListForRoomType(int projectId, int? roomTypeId, string export)
    {
        var claims = await claimsRepository.GetClaimsForRoomType(projectId, ClaimStatusSpec.Active, roomTypeId);

        string title;
        if (roomTypeId == null)
        {
            title = "Заявки без поселения";
        }
        else
        {
            title = "Заявки с поселением:" +
                    (await accommodationRepository.GetRoomTypeById(roomTypeId.Value)).Name;
        }

        return await ShowMasterClaimList(projectId, export, title, claims, ClaimStatusSpec.Active);
    }

    [HttpGet("~/{ProjectId}/roles/{CharacterGroupId}/discussing")]
    [MasterAuthorize()]
    public ActionResult ListForGroupDirect(int projectId, int characterGroupId, string export)
    {
        return RedirectToActionPermanent("Details", "Game", new { ProjectId = projectId });
    }

    [HttpGet("~/{ProjectId}/roles/{CharacterGroupId}/claims")]
    [MasterAuthorize()]
    public async Task<ActionResult> ListForGroup(int projectId, int characterGroupId, string export)
    {
        var group = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);

        if (group == null)
        {
            return NotFound();
        }
        var groupIds = group.GetChildrenGroupsIdRecursiveIncludingThis();
        var claims = await claimsRepository.GetClaimsForGroups(projectId, ClaimStatusSpec.Active, groupIds);

        return await ShowMasterClaimListForGroup(group, export, "Заявки в группу (все)", claims,
            GroupNavigationPage.ClaimsActive, ClaimStatusSpec.Active);
    }

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> DiscussingForGroup(int projectId, int characterGroupId, string export)
    {
        var group = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
        if (group == null)
        {
            return NotFound();
        }
        var groupIds = group.GetChildrenGroupsIdRecursiveIncludingThis();
        var claims = await claimsRepository.GetClaimsForGroups(projectId, ClaimStatusSpec.Discussion, groupIds);

        return await ShowMasterClaimListForGroup(group, export, "Обсуждаемые заявки в группу (все)",
            claims, GroupNavigationPage.ClaimsDiscussing, ClaimStatusSpec.Active);
    }

    #region Responsible

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> ResponsibleDiscussing(int projectid, int responsibleMasterId, string export)
        => await ShowMasterClaimList(projectid, export, "Обсуждаемые заявки на мастере", ClaimStatusSpec.Discussion, responsibleMasterId);

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> ResponsibleOnHold(int projectid, int responsiblemasterid, string export)
        => await ShowMasterClaimList(projectid, export, "Лист ожидания на мастере", ClaimStatusSpec.OnHold, responsiblemasterid);

    [HttpGet("~/{ProjectId}/claims/for-master/{ResponsibleMasterId}")]
    [MasterAuthorize()]

    public async Task<ActionResult> Responsible(int projectid, int responsibleMasterId, string export)
        => await ShowMasterClaimList(projectid, export, "Заявки на мастере", ClaimStatusSpec.Active, responsibleMasterId);

    [HttpGet("~/{ProjectId}/claims/problems-for-master/{ResponsibleMasterId}")]
    [MasterAuthorize()]
    public async Task<ActionResult> ResponsibleProblems(int projectId, int responsibleMasterId, string export)
        => await ShowMasterClaimList(projectId, export, "Проблемные заявки на мастере", ClaimStatusSpec.Any, responsibleMasterId,
            (claim, projectInfo) => claimValidator.Validate(claim, projectInfo, ProblemSeverity.Warning).Any());
    #endregion

    #region By Status

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> ActiveList(int projectId, string export) => await ShowMasterClaimList(projectId, export, "Активные заявки", ClaimStatusSpec.Active);

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> DeclinedList(int projectId, string export) => await ShowMasterClaimList(projectId, export, "Отклоненные/отозванные заявки", ClaimStatusSpec.InActive);

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> Discussing(int projectId, string export) => await ShowMasterClaimList(projectId, export, "Обсуждаемые заявки", ClaimStatusSpec.Discussion);

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> OnHoldList(int projectid, string export) => await ShowMasterClaimList(projectid, export, "Лист ожидания", ClaimStatusSpec.OnHold);
    #endregion

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> WaitingForFee(int projectid, string export)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectid));

        var claims =
            (await claimsRepository.GetClaims(projectid, ClaimStatusSpec.Approved))
            .Where(claim => !claim.ClaimPaidInFull(projectInfo))
            .ToList();

        return await ShowMasterClaimList(projectid, export, "Неоплаченные принятые заявки", claims, ClaimStatusSpec.Approved);
    }

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> SomeFieldsToFill(int projectid, string export)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(projectid));
        var playerEditableFields = projectInfo.UnsortedFields.Where(p => p.CanPlayerEdit).Select(c => c.Id).ToList();
        var loadedClaims = await claimsRepository.GetClaims(projectid, ClaimStatusSpec.Approved);
        var claims =
            loadedClaims
            .Where(claim => claimValidator.ValidateFieldsOnly(claim, projectInfo, playerEditableFields).Any())
            .ToList();

        return await ShowMasterClaimList(projectid, export, "Заявки с незаполненными полями", claims, ClaimStatusSpec.Approved);
    }

    [HttpGet("~/my/claims")]
    [Authorize]
    public async Task<ActionResult> My(string? export)
    {
        var user = await GetCurrentUserAsync();
        var title = "Мои заявки";

        var exportType = ExportTypeNameParserHelper.ToExportType(export);

        var claims = user.Claims.ToList();
        var projectInfos = new List<ProjectInfo>();
        foreach (var projectId in claims.Select(c => c.ProjectId).Distinct())
        {
            projectInfos.Add(await projectMetadataRepository.GetProjectMetadata(new(projectId)));
        }

        if (exportType == null)
        {
            var viewModel = new MyClaimListViewModel(
                CurrentUserId,
                claims,
                title: "Мои заявки",
                unreadComments: new Dictionary<int, int>()); ; //TODO Pass unread info here
            return base.View("Index", viewModel);
        }
        else
        {
            var viewModel = new ClaimListForExportViewModel(
                CurrentUserId,
                claims.Select(c => (c, projectInfos.Single(pi => pi.ProjectId.Value == c.ProjectId))).ToList());
            return ExportWithCustomFrontend(
                viewModel.Items,
                title,
                exportType.Value,
                new MyClaimListItemViewModelExporter(uriService),
                user.GetDisplayName());
        }
    }

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> Problems(int projectId, string export)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));
        var claims =
            (await claimsRepository.GetClaims(projectId, ClaimStatusSpec.Any))
            .Where(c => claimValidator.Validate(c, projectInfo, ProblemSeverity.Warning).Any()).ToList();
        return
            await ShowMasterClaimList(projectId, export, "Проблемные заявки", claims, ClaimStatusSpec.Any);
    }



    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> PaidDeclined(int projectid, string export)
    {
        var claims =
            (await claimsRepository.GetClaims(projectid, ClaimStatusSpec.InActive))
            .Where(claim => claim.ClaimBalance() > 0)
            .ToList();

        return await ShowMasterClaimList(projectid, export, "Оплаченные отклоненные заявки", claims, ClaimStatusSpec.InActive);
    }

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> ByAssignedField(int projectfieldid, int projectid, string export)
    {
        var claims = await claimsRepository.GetClaims(projectid, ClaimStatusSpec.Active);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectid));
        var fieldId = new ProjectFieldIdentification(new(projectid), projectfieldid);
        var field = projectInfo.GetFieldById(fieldId);

        return await ShowMasterClaimList(projectid, export, "Поле (проставлено): " + field.Name, claims.Where(c => c.GetSingleField(projectInfo, fieldId)!.HasEditableValue)
                .ToList(),
                ClaimStatusSpec.Active
        );
    }

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> ByUnAssignedField(int projectfieldid, int projectid, string export)
    {
        var claims = await claimsRepository.GetClaims(projectid, ClaimStatusSpec.Active);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectid));
        var fieldId = new ProjectFieldIdentification(new(projectid), projectfieldid);
        var field = projectInfo.GetFieldById(fieldId);

        return await ShowMasterClaimList(projectid, export, "Поле (непроставлено): " + field.Name, claims.Where(c => !c.GetSingleField(projectInfo, fieldId)!.HasEditableValue)
                .ToList(),
                ClaimStatusSpec.Active
        );
    }
    private FileContentResult ExportWithCustomFrontend(
        IEnumerable<ClaimListItemForExportViewModel> viewModel, string title,
        ExportType exportType, IGeneratorFrontend<ClaimListItemForExportViewModel> frontend, string projectName)
    {
        var generator = exportDataService.GetGenerator(exportType, viewModel,
          frontend);

        return GeneratorResultHelper.Result(projectName + ": " + title, generator);
    }
}
