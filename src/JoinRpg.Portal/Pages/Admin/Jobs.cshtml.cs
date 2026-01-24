using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Portal.Infrastructure.DailyJobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.Admin;

[AdminAuthorize]
public class JobsModel(IEnumerable<IJobRunner> dailyJobs) : PageModel
{
    public void OnGet()
    {
        string[] jobNames = [.. dailyJobs.Select(j => j.Name)];

        Jobs = [.. jobNames.Select(n => new JobInfoViewModel(n))];
    }

    public async Task<IActionResult> OnPost(string name, [FromServices] IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var jobRunner = dailyJobs.Single(j => j.Name == name);
        await jobRunner.RunJob(scope, cancellationToken);
        return RedirectToPage();
    }

    public JobInfoViewModel[] Jobs { get; set; } = null!;

}

public record class JobInfoViewModel(string Name);
