using System.Diagnostics;

namespace JoinRpg.Portal.Infrastructure.DailyJobs;

public static class BackgroundServiceActivity
{
    public const string ActivitySourceName = "BackgroundJobService";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
