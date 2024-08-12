using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.Admin;

[AdminAuthorize]
public class JobsModel(IEnumerable<IDailyJob> dailyJobs) : PageModel
{
    public void OnGet()
    {
        string[] jobNames = [.. dailyJobs.Select(j => j.GetType().Name)];

        Jobs = [.. jobNames.Select(n => new JobInfoViewModel(n))];
    }

    public async Task<IActionResult> OnPost(string name, CancellationToken cancellationToken)
    {
        var job = dailyJobs.Single(j => j.GetType().Name == name);
        await job.RunOnce(cancellationToken);
        return RedirectToPage();
    }

    public JobInfoViewModel[] Jobs { get; set; } = null!;

}

public record class JobInfoViewModel(string Name);
