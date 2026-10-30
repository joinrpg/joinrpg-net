using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.Claims.UnifiedGrid;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.Web.Models.Exporters;
using JoinRpg.WebPortal.Managers.UnifiedGrid;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{ProjectId}/claims/[action]")]
public class ClaimListController(
    IClaimsRepository claimsRepository,
    IClaimProblemValidator claimValidator,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor,
    ClaimListExportService claimListExportService
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
        claimInfos ??= await claimListExportService.LoadClaimInfos(claims);

        IReadOnlyList<ClaimInfo> claimsList = [.. claims.Select(c => claimInfos[c.GetId()])];

        if (exportType == null)
        {
            var unreadComments = await claimsRepository.GetUnreadDiscussionsForClaims(projectId, claimStatusSpec, currentUserAccessor.UserId, hasMasterAccess: true);

            var view = new ClaimListViewModel(currentUserAccessor, claimsList, unreadComments, title, projectInfo, claimValidator);
            return View("Index", view);
        }
        else
        {
            return await claimListExportService.ExportClaimList(projectId, claimsList, title, exportType.Value);
        }
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
        var claimInfos = await claimListExportService.LoadClaimInfos(claims);

        return await ___ShowMasterClaimList(projectId, export, title,
            [.. claims.Where(c => predicate(claimInfos[c.GetId()]))], claimStatusSpec, claimInfos);

    }

    #endregion

    #region Редиректы на грид (ADR027 §6)

    /// <summary>
    /// Эти списки переехали на грид <c>/{projectId}/claims</c>: старый адрес отвечает 301, потому
    /// что на списки и их выгрузки бывают внешние закладки. Параметры <c>export</c> и <c>page</c>
    /// приезжали query-string'ом — пробрасываем, если пришли.
    /// </summary>
    private RedirectResult RedirectToClaimGrid(
        ProjectIdentification projectId,
        UgStatusFilterView? status = null,
        UserIdentification? responsibleMasterId = null,
        CharacterGroupIdentification? characterGroupId = null,
        bool onlyWithProblems = false,
        string? export = null)
        => RedirectPermanent(ClaimGridUrls.Build(
            projectId,
            status,
            responsibleMasterId,
            characterGroupId,
            onlyWithProblems,
            PageFromQuery(),
            export));

    private int? PageFromQuery()
        => int.TryParse(Request.Query[UgQueryParameterNames.Page], out var page) ? page : null;

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
    public ActionResult ListForGroupDirect(CharacterGroupIdentification characterGroupId, string export)
        => RedirectToActionPermanent("Details", "Game", new { ProjectId = characterGroupId.ProjectId.Value });

    [HttpGet("~/{ProjectId}/roles/{CharacterGroupId}/claims")]
    [MasterAuthorize()]
    public ActionResult ListForGroup(CharacterGroupIdentification characterGroupId, string? export)
        => RedirectToClaimGrid(characterGroupId.ProjectId, characterGroupId: characterGroupId, export: export);

    [HttpGet, MasterAuthorize()]
    public ActionResult DiscussingForGroup(CharacterGroupIdentification characterGroupId, string? export)
        => RedirectToClaimGrid(characterGroupId.ProjectId, UgStatusFilterView.Discussion, characterGroupId: characterGroupId, export: export);

    #region Responsible

    [HttpGet, MasterAuthorize()]
    public ActionResult ResponsibleDiscussing(ProjectIdentification projectid, UserIdentification responsibleMasterId, string? export)
        => RedirectToClaimGrid(projectid, UgStatusFilterView.Discussion, responsibleMasterId: responsibleMasterId, export: export);

    [HttpGet, MasterAuthorize()]
    public ActionResult ResponsibleOnHold(ProjectIdentification projectid, UserIdentification responsibleMasterId, string? export)
        => RedirectToClaimGrid(projectid, UgStatusFilterView.OnHold, responsibleMasterId: responsibleMasterId, export: export);

    [HttpGet("~/{ProjectId}/claims/for-master/{ResponsibleMasterId}")]
    [MasterAuthorize()]
    public ActionResult Responsible(ProjectIdentification projectid, UserIdentification responsibleMasterId, string? export)
        => RedirectToClaimGrid(projectid, responsibleMasterId: responsibleMasterId, export: export);

    [HttpGet("~/{ProjectId}/claims/problems-for-master/{ResponsibleMasterId}")]
    [MasterAuthorize()]
    public ActionResult ResponsibleProblems(ProjectIdentification projectId, UserIdentification responsibleMasterId, string? export)
        // Старый список брал заявки любых статусов, поэтому статус «Все»: проблемы отклонённых
        // заявок и удалённых персонажей не должны пропасть (ADR027 §6).
        => RedirectToClaimGrid(projectId, UgStatusFilterView.All, responsibleMasterId: responsibleMasterId, onlyWithProblems: true, export: export);
    #endregion

    #region By Status

    [HttpGet, MasterAuthorize()]
    public ActionResult ActiveList(ProjectIdentification projectId, string? export)
        => RedirectToClaimGrid(projectId, export: export);

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> DeclinedList(ProjectIdentification projectId, string export) => await ShowMasterClaimList(projectId, export, "Отклоненные/отозванные заявки", ClaimStatusSpec.InActive);

    [HttpGet, MasterAuthorize()]
    public ActionResult Discussing(ProjectIdentification projectId, string? export)
        => RedirectToClaimGrid(projectId, UgStatusFilterView.Discussion, export: export);

    [HttpGet, MasterAuthorize()]
    public ActionResult OnHoldList(ProjectIdentification projectid, string? export)
        => RedirectToClaimGrid(projectid, UgStatusFilterView.OnHold, export: export);
    #endregion

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> WaitingForFee(ProjectIdentification projectid, string export)
    {
        var loadedClaims = await claimsRepository.GetClaims(projectid, ClaimStatusSpec.Approved);
        // Отбор и строка списка считают баланс одним расчётом (ADR021): иначе при расхождении правил
        // в «неоплаченных» оказалась бы строка с нулевым остатком.
        var claimInfos = await claimListExportService.LoadClaimInfos(loadedClaims);
        var claims =
            loadedClaims
            .Where(claim => claimInfos[claim.GetId()].ClaimInCharacter.CalculateBalance().FeeDue > 0)
            .ToList();

        return await ShowMasterClaimList(projectid, export, "Неоплаченные принятые заявки", claims, ClaimStatusSpec.Approved, claimInfos);
    }

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> SomeFieldsToFill(ProjectIdentification projectid, string export)
    {
        var loadedClaims = await claimsRepository.GetClaims(projectid, ClaimStatusSpec.Approved);
        var claimInfos = await claimListExportService.LoadClaimInfos(loadedClaims);
        var claims =
            loadedClaims
            .Where(claim => claimValidator.ValidatePlayerToFill(claimInfos[claim.GetId()]).Any())
            .ToList();

        return await ShowMasterClaimList(projectid, export, "Заявки с незаполненными полями", claims, ClaimStatusSpec.Approved, claimInfos);
    }

    [HttpGet, MasterAuthorize()]
    public ActionResult Problems(ProjectIdentification projectId, string? export)
        // Старый список брал заявки любых статусов, поэтому статус «Все»: проблемы отклонённых
        // заявок и удалённых персонажей не должны пропасть (ADR027 §6).
        => RedirectToClaimGrid(projectId, UgStatusFilterView.All, onlyWithProblems: true, export: export);



    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> PaidDeclined(ProjectIdentification projectid, string export)
    {
        var loadedClaims = await claimsRepository.GetClaims(projectid, ClaimStatusSpec.InActive);
        var claimInfos = await claimListExportService.LoadClaimInfos(loadedClaims);
        var claims =
            loadedClaims
            .Where(claim => claimInfos[claim.GetId()].ClaimInCharacter.Claim.Finance.FeePaid > 0)
            .ToList();

        return await ShowMasterClaimList(projectid, export, "Оплаченные отклоненные заявки", claims, ClaimStatusSpec.InActive, claimInfos);
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
}
