using JoinRpg.Web.Claims.UnifiedGrid;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.GamePages;

public class CaptainCabinetModel : PageModel
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

    public void OnGet()
    {
    }

    [BindProperty(SupportsGet = true)]
    public required ProjectIdentification ProjectId { get; set; }
}
