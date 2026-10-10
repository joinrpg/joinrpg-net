using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Portal.Infrastructure.DailyJobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Pages.Admin;

[AdminAuthorize]
public class JobsModel(IEnumerable<IJobRunner> dailyJobs, IOptionsMonitor<DailyJobsOptions> options) : PageModel
{
    public void OnGet()
    {
        var current = options.CurrentValue;
        Jobs = [.. dailyJobs.Select(j => new JobInfoViewModel(j.Name, j.FullName, current.IsDisabled(j.JobType)))];
    }

    public async Task<IActionResult> OnPost(string name, [FromServices] IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var jobRunner = dailyJobs.Single(j => j.Name == name);
        if (options.CurrentValue.IsDisabled(jobRunner.JobType))
        {
            ErrorMessage = $"Джоба {jobRunner.Name} выключена в конфигурации ({DailyJobsOptions.SectionName}:Disabled) и не может быть запущена.";
            OnGet();
            return Page();
        }

        using var scope = serviceProvider.CreateScope();
        using var activity = BackgroundServiceActivity.ActivitySource.StartActivity($"Run of {jobRunner.FullName}");
        activity?.AddTag("jobName", jobRunner.FullName);
        await jobRunner.RunJob(scope, cancellationToken);
        return RedirectToPage();
    }

    public JobInfoViewModel[] Jobs { get; set; } = null!;

    public string? ErrorMessage { get; set; }

}

public record class JobInfoViewModel(string Name, string FullName, bool IsDisabledByConfig);
