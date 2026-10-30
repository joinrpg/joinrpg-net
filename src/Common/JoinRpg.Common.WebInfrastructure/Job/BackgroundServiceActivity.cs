using System.Diagnostics;

namespace JoinRpg.Common.WebInfrastructure.Job;

/// <summary>
/// ActivitySource для запусков фоновых джоб (имя «BackgroundJobService»). Подключать в телеметрию хоста.
/// </summary>
public static class BackgroundServiceActivity
{
    public const string ActivitySourceName = "BackgroundJobService";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
