using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.GamePages;

/// <summary>
/// Восстановление проекта: заявки из уведомлений (DRP, #5481), группы, персонажи и поля из выгрузок
/// мастера (#5529). URL прежний, с «restore-from-notifications». Работает только в заблокированном проекте (ADR023),
/// поэтому <c>[ProjectShouldBeActive]</c> тут нет — он такие проекты отвергает.
/// </summary>
[RequireMaster(Permission.CanManageClaims)]
public class ClaimRestoreModel : PageModel
{
    public void OnGet()
    {
    }

    [BindProperty(SupportsGet = true)]
    public required ProjectIdentification ProjectId { get; set; }
}
