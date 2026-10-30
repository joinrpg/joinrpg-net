using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Claims.UnifiedGrid;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.Web.Models.Exporters;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.WebPortal.Managers.UnifiedGrid;

/// <summary>
/// Выгрузка списка заявок в Excel (ADR027 §5). Раньше собиралась private-методами в
/// <c>ClaimListController</c>, теперь один сервис и для старых списков, и для грида заявок.
/// </summary>
public class ClaimListExportService(
    IClaimInfoRepository claimInfoRepository,
    IRoomCategoryPlanRepository roomCategoryPlanRepository,
    IProjectMetadataRepository projectMetadataRepository,
    IExportDataService exportDataService,
    IUriService uriService,
    ICurrentUserAccessor currentUserAccessor,
    IUnifiedGridClient gridClient)
{
    /// <summary>
    /// <see cref="ClaimInfo"/> на весь список — заявки вместе с персонажами и профилями игроков,
    /// две выборки, а не по заявке.
    /// </summary>
    public Task<IReadOnlyDictionary<ClaimIdentification, ClaimInfo>> LoadClaimInfos(
        IReadOnlyCollection<Claim> claims)
        => claimInfoRepository.GetClaimInfos([.. claims.Select(c => c.GetId())]);

    /// <summary>
    /// Выгрузка уже отобранного списка заявок старым пайплайном: колонки не меняются — по их
    /// заголовкам разбирает выгрузку DRP (<c>MasterExportColumns</c>).
    /// </summary>
    public async Task<FileContentResult> ExportClaimList(
        ProjectIdentification projectId,
        IReadOnlyList<ClaimInfo> claims,
        string title,
        ExportType exportType)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var view = new ClaimListForExportViewModel(
            currentUserAccessor,
            claims,
            await LoadRoomNames(projectInfo, claims));

        return ExportWithCustomFrontend(
            view.Items, title, exportType, new ClaimListItemViewModelExporter(uriService, projectInfo), projectInfo.ProjectName);
    }

    /// <summary>
    /// Выгрузка грида заявок (ADR027 §5): фильтр пересчитывается без пейджинга, и по всему
    /// результату строится тот же файл, что у старых списков.
    /// </summary>
    public async Task<FileContentResult> ExportGrid(ProjectIdentification projectId, UgFilterView filter, ExportType exportType)
    {
        // Фильтр пересчитываем тем же путём, что и грид: OperationClaimIds покрывают весь
        // результат без пейджинга, а ClaimInfo для выгрузки берёт уже существующий репозиторий.
        var claimIds = (await gridClient.GetGrid(projectId, UgScopeView.Master, filter, page: 1)).OperationClaimIds;
        return await ExportClaimList(
            projectId,
            [.. (await claimInfoRepository.GetClaimInfos(claimIds)).Values],
            BuildTitle(filter, await projectMetadataRepository.GetProjectMetadata(projectId)),
            exportType);
    }

    /// <summary>
    /// Заголовок файла выгрузки грида — из фильтра: по файлу видно, какой список в нём лежит.
    /// </summary>
    internal static string BuildTitle(UgFilterView filter, ProjectInfo projectInfo)
    {
        // Короткое имя статуса, а не описание: в имени файла место дорого, а список статусов
        // виден и на самой странице.
        var parts = new List<string> { filter.Status.GetDisplayName() };
        if (filter.Group is { } group && projectInfo.GroupTree.GetGroupByIdOrDefault(group) is { } groupInfo)
        {
            parts.Add($"группа «{groupInfo.Name}»");
        }
        // Мастера может не быть в метаданных (снят, закладка со старым id) — как с группой,
        // часть заголовка просто пропускаем, а не роняем выгрузку.
        if (filter.Responsible is { } responsible
            && projectInfo.Masters.FirstOrDefault(master => master.UserId == responsible) is { } masterInfo)
        {
            parts.Add($"ответственный — {masterInfo.Name.DisplayName}");
        }
        if (filter.OnlyWithProblems)
        {
            parts.Add("только с проблемами");
        }
        return "Заявки: " + string.Join(", ", parts);
    }

    /// <summary>
    /// Комнаты расселённых заявок для выгрузки — из планов поселения одним запросом (ADR022). Если
    /// ни одна заявка списка в группе проживающих не состоит, планы не грузятся вовсе.
    /// </summary>
    private async Task<IReadOnlyDictionary<ClaimIdentification, string>> LoadRoomNames(
        ProjectInfo projectInfo,
        IReadOnlyCollection<ClaimInfo> claims)
    {
        if (!projectInfo.AccommodationSettings.Enabled || !claims.Any(c => c.Claim.AccommodationTypeId != null))
        {
            return new Dictionary<ClaimIdentification, string>();
        }

        // Индекс «заявка → группа» уже есть внутри плана — спрашиваем FindRoomByClaim, как печать
        // конвертов (PrintViewService).
        var plans = await roomCategoryPlanRepository.GetAllPlans(projectInfo.ProjectId);
        return claims
            .Select(claim => claim.ClaimId)
            .Select(claimId => (ClaimId: claimId, Room: plans
                .Select(plan => plan.FindRoomByClaim(claimId))
                .FirstOrDefault(room => room is not null)))
            .Where(pair => pair.Room is not null)
            .ToDictionary(pair => pair.ClaimId, pair => pair.Room!.Name);
    }

    private FileContentResult ExportWithCustomFrontend(
        IEnumerable<ClaimListItemForExportViewModel> viewModel, string title,
        ExportType exportType, IGeneratorFrontend<ClaimListItemForExportViewModel> frontend, string projectName)
    {
        var generator = exportDataService.GetGenerator(exportType, viewModel, frontend);

        return new FileContentResult(generator.Generate(), generator.ContentType)
        {
            FileDownloadName = Path.ChangeExtension((projectName + ": " + title).ToSafeFileName(), generator.FileExtension),
        };
    }
}
