using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Helpers;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.Web.Models.Exporters;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{ProjectId}/claims/[action]")]
public class ClaimListController(
    IExportDataService exportDataService,
    IClaimsRepository claimsRepository,
    IUriService uriService,
    IClaimProblemValidator claimValidator,
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterGroupRepository charGroupRepository,
    IClaimInfoRepository claimInfoRepository,
    ICurrentUserAccessor currentUserAccessor,
    IRoomCategoryPlanRepository roomCategoryPlanRepository
        ) : Common.JoinControllerGameBase
{

    #region implementation

    /// <param name="claimInfos">
    /// Уже загруженные <see cref="ClaimInfo"/>, если вызывающий строил их для отбора заявок. Иначе
    /// собираются здесь. Передавать стоит: иначе на страницах, которые сами фильтруют по
    /// проблемам, персонажи и профили грузились бы дважды.
    /// </param>
    private async Task<ActionResult> ___ShowMasterClaimList(
        ProjectIdentification projectId,
        string export,
        string title,
        IReadOnlyCollection<Claim> claims,
        ClaimStatusSpec claimStatusSpec,
        IReadOnlyDictionary<ClaimIdentification, ClaimInfo>? claimInfos = null)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var exportType = ExportTypeNameParserHelper.ToExportType(export);
        claimInfos ??= await LoadClaimInfos(claims);

        if (exportType == null)
        {
            var unreadComments = await claimsRepository.GetUnreadDiscussionsForClaims(projectId, claimStatusSpec, currentUserAccessor.UserId, hasMasterAccess: true);
            var view = new ClaimListViewModel(currentUserAccessor, claims, unreadComments, title, projectInfo, claimValidator, claimInfos);
            return View("Index", view);
        }
        else
        {
            var view = new ClaimListForExportViewModel(currentUserAccessor, claims, claimInfos,
                await LoadRoomNames(projectInfo, claims));

            return
                    ExportWithCustomFrontend(view.Items, title, exportType.Value,
                        new ClaimListItemViewModelExporter(uriService, projectInfo), projectInfo.ProjectName);
        }
    }

    /// <summary>
    /// <see cref="ClaimInfo"/> на весь список — заявки вместе с персонажами и профилями игроков,
    /// две выборки, а не по заявке.
    /// </summary>
    private Task<IReadOnlyDictionary<ClaimIdentification, ClaimInfo>> LoadClaimInfos(
        IReadOnlyCollection<Claim> claims)
        => claimInfoRepository.GetClaimInfos([.. claims.Select(c => c.GetId())]);

    /// <summary>
    /// Комнаты расселённых заявок для выгрузки — из планов поселения одним запросом (ADR022). Если
    /// ни одна заявка списка в группе проживающих не состоит, планы не грузятся вовсе.
    /// </summary>
    private async Task<IReadOnlyDictionary<ClaimIdentification, string>> LoadRoomNames(
        ProjectInfo projectInfo,
        IReadOnlyCollection<Claim> claims)
    {
        if (!projectInfo.AccommodationSettings.Enabled || !claims.Any(c => c.AccommodationRequest_Id != null))
        {
            return new Dictionary<ClaimIdentification, string>();
        }

        // Индекс «заявка → группа» уже есть внутри плана — спрашиваем FindRoomByClaim, как печать
        // конвертов (PrintViewService).
        var plans = await roomCategoryPlanRepository.GetAllPlans(projectInfo.ProjectId);
        return claims
            .Select(claim => claim.GetId())
            .Select(claimId => (ClaimId: claimId, Room: plans
                .Select(plan => plan.FindRoomByClaim(claimId))
                .FirstOrDefault(room => room is not null)))
            .Where(pair => pair.Room is not null)
            .ToDictionary(pair => pair.ClaimId, pair => pair.Room!.Name);
    }

    private async Task<ActionResult> ShowMasterClaimList(ProjectIdentification projectId, string export, string title, IReadOnlyCollection<Claim> claims, ClaimStatusSpec claimStatusSpec,
        IReadOnlyDictionary<ClaimIdentification, ClaimInfo>? claimInfos = null)
    {

        return await ___ShowMasterClaimList(projectId, export, title, claims, claimStatusSpec, claimInfos);
    }

    private async Task<ActionResult> ShowMasterClaimList(ProjectIdentification projectId, string export, string title, ClaimStatusSpec claimStatusSpec)
    {
        var claims = await claimsRepository.GetClaims(projectId, claimStatusSpec);

        return await ___ShowMasterClaimList(projectId, export, title, claims, claimStatusSpec);

    }

    private async Task<ActionResult> ShowMasterClaimList(ProjectIdentification projectId, string export, string title, ClaimStatusSpec claimStatusSpec, int masterUserId)
    {
        var claims = await claimsRepository.GetClaimsForMaster(projectId, masterUserId, claimStatusSpec);

        return await ___ShowMasterClaimList(projectId, export, title, claims, claimStatusSpec);

    }

    /// <param name="predicate">
    /// Отбор по проблемам: считается по доменному контексту заявки, в SQL не выражается.
    /// Контексты собираются один раз и переиспользуются при отрисовке списка.
    /// </param>
    private async Task<ActionResult> ShowMasterClaimList(ProjectIdentification projectId, string export, string title, ClaimStatusSpec claimStatusSpec, int masterUserId, Func<ClaimInfo, bool> predicate)
    {
        var claims = await claimsRepository.GetClaimsForMaster(projectId, masterUserId, claimStatusSpec);
        var claimInfos = await LoadClaimInfos(claims);

        return await ___ShowMasterClaimList(projectId, export, title,
            [.. claims.Where(c => predicate(claimInfos[c.GetId()]))], claimStatusSpec, claimInfos);

    }

    private async Task<ActionResult> ShowMasterClaimListForGroup(CharacterGroupFullInfo? characterGroup, string export,
        string title, IReadOnlyCollection<Claim> claims, GroupNavigationPage page, ClaimStatusSpec claimStatusSpec)
    {
        if (characterGroup == null)
        {
            return NotFound();
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(characterGroup.Id.ProjectId);

        var exportType = ExportTypeNameParserHelper.ToExportType(export);
        var claimInfos = await LoadClaimInfos(claims);

        if (exportType == null)
        {
            var unreadComments = await claimsRepository.GetUnreadDiscussionsForClaims(characterGroup.Id.ProjectId.Value, claimStatusSpec, currentUserAccessor.UserId, hasMasterAccess: true);
            var view = new ClaimListForGroupViewModel(currentUserAccessor, claims, characterGroup, page, unreadComments, claimValidator,
                claimInfos, projectInfo, title);
            return View("ByGroup", view);
        }
        else
        {
            var view = new ClaimListForExportViewModel(currentUserAccessor, claims, claimInfos,
                await LoadRoomNames(projectInfo, claims));
            return
                    ExportWithCustomFrontend(view.Items, title, exportType.Value,
                        new ClaimListItemViewModelExporter(uriService, projectInfo),
                        projectInfo.ProjectName);
        }
    }

    #endregion

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> ForPlayer(ProjectIdentification projectId, int userId, string export)
    {
        var claims = await claimsRepository.GetClaimsForPlayer(projectId, ClaimStatusSpec.Active, userId);

        return await ShowMasterClaimList(projectId, export, "Заявки на игроке", claims, ClaimStatusSpec.Active);
    }

    [HttpGet("~/{ProjectId}/claims/without-roomtype")]
    [MasterAuthorize()]
    public Task<ActionResult> ListWithoutRoomType(ProjectIdentification projectId, string export) =>
        ListForRoomType(projectId, null, export);

    [HttpGet("~/{ProjectId}/claims/by-roomtype/{roomTypeId?}")]
    [MasterAuthorize()]
    public async Task<ActionResult> ListForRoomType(ProjectIdentification projectId, int? roomTypeId, string export)
    {
        var claims = await claimsRepository.GetClaimsForRoomType(projectId, ClaimStatusSpec.Active, roomTypeId);

        string title;
        if (roomTypeId == null)
        {
            title = "Заявки без поселения";
        }
        else
        {
            var projectMetadata = await projectMetadataRepository.GetProjectMetadata(projectId);
            // OrDefault, а не GetTypeById: id приходит параметром фильтра, и промах — это 404,
            // а не исключение. Раньше запрос за типом вообще не учитывал проект и падал 500-й.
            var roomType = projectMetadata.AccommodationSettings.GetTypeByIdOrDefault(
                new AccommodationTypeIdentification(projectId, roomTypeId.Value));
            if (roomType is null)
            {
                return NotFound();
            }

            title = "Заявки с поселением:" + roomType.Name;
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
    public async Task<ActionResult> ListForGroup(ProjectIdentification projectId, int characterGroupId, string export)
    {
        var characterGroupId2 = new CharacterGroupIdentification(projectId, characterGroupId);
        var characterGroup = await charGroupRepository.GetCharacterGroupFullInfo(characterGroupId2);

        if (characterGroup == null)
        {
            return NotFound();
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var groupIds = projectInfo.GroupTree.GetChildGroupIdsIncludingThis(characterGroupId2).ToArray();
        var claims = await claimsRepository.GetClaimsForGroups(projectId, ClaimStatusSpec.Active, groupIds);

        return await ShowMasterClaimListForGroup(characterGroup, export, "Заявки в группу (все)", claims,
            GroupNavigationPage.ClaimsActive, ClaimStatusSpec.Active);
    }

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> DiscussingForGroup(ProjectIdentification projectId, int characterGroupId, string export)
    {
        var characterGroupId2 = new CharacterGroupIdentification(projectId, characterGroupId);
        var characterGroup = await charGroupRepository.GetCharacterGroupFullInfo(characterGroupId2);

        if (characterGroup == null)
        {
            return NotFound();
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var groupIds = projectInfo.GroupTree.GetChildGroupIdsIncludingThis(characterGroupId2).ToArray();
        var claims = await claimsRepository.GetClaimsForGroups(projectId, ClaimStatusSpec.Discussion, groupIds);

        return await ShowMasterClaimListForGroup(characterGroup, export, "Обсуждаемые заявки в группу (все)",
            claims, GroupNavigationPage.ClaimsDiscussing, ClaimStatusSpec.Active);
    }

    #region Responsible

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> ResponsibleDiscussing(ProjectIdentification projectid, int responsibleMasterId, string export)
        => await ShowMasterClaimList(projectid, export, "Обсуждаемые заявки на мастере", ClaimStatusSpec.Discussion, responsibleMasterId);

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> ResponsibleOnHold(ProjectIdentification projectid, int responsiblemasterid, string export)
        => await ShowMasterClaimList(projectid, export, "Лист ожидания на мастере", ClaimStatusSpec.OnHold, responsiblemasterid);

    [HttpGet("~/{ProjectId}/claims/for-master/{ResponsibleMasterId}")]
    [MasterAuthorize()]

    public async Task<ActionResult> Responsible(ProjectIdentification projectid, int responsibleMasterId, string export)
        => await ShowMasterClaimList(projectid, export, "Заявки на мастере", ClaimStatusSpec.Active, responsibleMasterId);

    [HttpGet("~/{ProjectId}/claims/problems-for-master/{ResponsibleMasterId}")]
    [MasterAuthorize()]
    public async Task<ActionResult> ResponsibleProblems(ProjectIdentification projectId, int responsibleMasterId, string export)
        => await ShowMasterClaimList(projectId, export, "Проблемные заявки на мастере", ClaimStatusSpec.Any, responsibleMasterId,
            context => claimValidator.Validate(context, ProblemSeverity.Warning).Any());
    #endregion

    #region By Status

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> ActiveList(ProjectIdentification projectId, string export) => await ShowMasterClaimList(projectId, export, "Активные заявки", ClaimStatusSpec.Active);

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> DeclinedList(ProjectIdentification projectId, string export) => await ShowMasterClaimList(projectId, export, "Отклоненные/отозванные заявки", ClaimStatusSpec.InActive);

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> Discussing(ProjectIdentification projectId, string export) => await ShowMasterClaimList(projectId, export, "Обсуждаемые заявки", ClaimStatusSpec.Discussion);

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> OnHoldList(ProjectIdentification projectid, string export) => await ShowMasterClaimList(projectid, export, "Лист ожидания", ClaimStatusSpec.OnHold);
    #endregion

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> WaitingForFee(ProjectIdentification projectid, string export)
    {
        var loadedClaims = await claimsRepository.GetClaims(projectid, ClaimStatusSpec.Approved);
        // Отбор и строка списка считают баланс одним расчётом (ADR021): иначе при расхождении правил
        // в «неоплаченных» оказалась бы строка с нулевым остатком.
        var claimInfos = await LoadClaimInfos(loadedClaims);
        var claims =
            loadedClaims
            .Where(claim => claimInfos[claim.GetId()].ClaimInCharacter.CalculateBalance().FeeDue > 0)
            .ToList();

        return await ShowMasterClaimList(projectid, export, "Неоплаченные принятые заявки", claims, ClaimStatusSpec.Approved, claimInfos);
    }

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> SomeFieldsToFill(ProjectIdentification projectid, string export)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(projectid));
        var playerEditableFields = projectInfo.UnsortedFields.Where(p => p.CanPlayerEdit).Select(c => c.Id).ToList();
        var loadedClaims = await claimsRepository.GetClaims(projectid, ClaimStatusSpec.Approved);
        var claimInfos = await LoadClaimInfos(loadedClaims);
        var claims =
            loadedClaims
            .Where(claim => claimValidator.ValidateFieldsOnly(claimInfos[claim.GetId()], playerEditableFields).Any())
            .ToList();

        return await ShowMasterClaimList(projectid, export, "Заявки с незаполненными полями", claims, ClaimStatusSpec.Approved, claimInfos);
    }

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> Problems(ProjectIdentification projectId, string export)
    {
        var loadedClaims = await claimsRepository.GetClaims(projectId, ClaimStatusSpec.Any);
        var claimInfos = await LoadClaimInfos(loadedClaims);
        var claims =
            loadedClaims
            .Where(c => claimValidator.Validate(claimInfos[c.GetId()], ProblemSeverity.Warning).Any()).ToList();
        return
            await ShowMasterClaimList(projectId, export, "Проблемные заявки", claims, ClaimStatusSpec.Any, claimInfos);
    }



    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> PaidDeclined(ProjectIdentification projectid, string export)
    {
        var claims =
            (await claimsRepository.GetClaims(projectid, ClaimStatusSpec.InActive))
            .Where(claim => claim.ClaimBalance() > 0)
            .ToList();

        return await ShowMasterClaimList(projectid, export, "Оплаченные отклоненные заявки", claims, ClaimStatusSpec.InActive);
    }

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> ByAssignedField(int projectfieldid, ProjectIdentification projectid, string export)
    {
        var claims = await claimsRepository.GetClaims(projectid, ClaimStatusSpec.Active);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectid));
        var fieldId = new ProjectFieldIdentification(projectid, projectfieldid);
        var field = projectInfo.GetFieldById(fieldId);

        return await ShowMasterClaimList(projectid, export, "Поле (проставлено): " + field.Name, claims.Where(c => c.GetSingleField(projectInfo, fieldId)!.HasEditableValue)
                .ToList(),
                ClaimStatusSpec.Active
        );
    }

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> ByUnAssignedField(int projectfieldid, ProjectIdentification projectid, string export)
    {
        var claims = await claimsRepository.GetClaims(projectid, ClaimStatusSpec.Active);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectid));
        var fieldId = new ProjectFieldIdentification(projectid, projectfieldid);
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
