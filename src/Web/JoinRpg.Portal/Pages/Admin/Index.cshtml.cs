using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.Admin;

[AdminAuthorize]
public class IndexModel(IProjectRestoreLookupRepository projectRestoreLookupRepository) : PageModel
{
    /// <summary>
    /// Сколько проектов заблокировал DRP 2026-10 (#5459): 119 активных проектов майской базы и 77 восстановленных
    /// оболочек (1623–1713). Признака «был заблокирован» в БД нет — после снятия блокировки следов не остаётся,
    /// поэтому число записано один раз по факту.
    /// </summary>
    public const int DrpInitiallyBlockedProjects = 119 + 77;

    public int BlockedProjects { get; private set; }

    public async Task OnGetAsync() => BlockedProjects = (await projectRestoreLookupRepository.GetBlockedProjects()).Count;
}
