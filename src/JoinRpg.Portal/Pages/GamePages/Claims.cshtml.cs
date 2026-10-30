using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.Claims.UnifiedGrid;
using JoinRpg.Web.Models.Exporters;
using JoinRpg.WebPortal.Managers.UnifiedGrid;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.GamePages;

/// <summary>
/// Грид заявок мастера (ADR027 §5): тот же остров, что в кабинете капитана, но с мастерским скоупом.
/// </summary>
[RequireMaster]
public class ClaimsModel(ClaimListExportService claimListExportService) : PageModel
{
    /// <summary>
    /// Фильтр статуса из query string (ADR027 §5): страница передаёт его острову параметром, а
    /// остров при смене фильтра переписывает URL. Мусорная строка биндинг не проходит — остров
    /// получает значение по умолчанию, страница не падает.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public UgStatusFilterView? Status { get; set; }

    /// <summary>Номер страницы из query string; вне диапазона сервер прижмёт к существующему.</summary>
    [BindProperty(SupportsGet = true)]
    public int? Page { get; set; }

    /// <summary>Мастерские фильтры из query string; имена — как у webapi GetGrid (ADR027 §5).</summary>
    [BindProperty(SupportsGet = true)]
    public bool OnlyWithProblems { get; set; }

    [BindProperty(SupportsGet = true)]
    public CharacterGroupIdentification? CharacterGroupId { get; set; }

    [BindProperty(SupportsGet = true)]
    public UserIdentification? ResponsibleMasterId { get; set; }

    /// <summary>
    /// Выгрузка того же фильтра в Excel (ADR027 §5): без параметра страница рисует остров, с ним —
    /// файл. На выгрузки бывают закладки, поэтому это query string, а не отдельный маршрут.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Export { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var exportType = ExportTypeNameParserHelper.ToExportType(Export);
        if (exportType is null)
        {
            return Page();
        }

        return await claimListExportService.ExportGrid(
            ProjectId,
            new UgFilterView(Status ?? UgStatusFilterView.Active, OnlyWithProblems, CharacterGroupId, ResponsibleMasterId),
            exportType.Value);
    }

    [BindProperty(SupportsGet = true)]
    public required ProjectIdentification ProjectId { get; set; }
}
