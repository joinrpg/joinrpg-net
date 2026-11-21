using JoinRpg.Blazor.Client.CommonPages;
using JoinRpg.Interfaces;
using JoinRpg.WebPortal.Managers.Budget;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Pages.About;

public class IndexModel(BudgetWidgetManager budgetWidgetManager, IOptions<BudgetOptions> budgetOptions) : PageModel
{
    public BudgetWidgetModel? BudgetWidget { get; private set; }

    public async Task OnGet()
    {
        BudgetWidget = await budgetWidgetManager.LoadWidgetModel(budgetOptions.Value.Project);
    }
}
