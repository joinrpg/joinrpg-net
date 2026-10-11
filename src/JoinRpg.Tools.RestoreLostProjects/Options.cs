using JoinRpg.Tools.Drp.Common;
using Microsoft.Extensions.Configuration;

namespace JoinRpg.Tools.RestoreLostProjects;

internal record Options(
    string MssqlConnectionString,
    string NotificationsConnectionString,
    int BackupMaxProjectId,
    DateTimeOffset LostAt,
    string ReportPath,
    IReadOnlyList<string> LogPaths)
{
    public const string ToolName = "restore-lost-projects";
    public const string BackupMaxOption = "--backup-max-project-id";
    public const string LogsOption = "--logs";

    /// <summary>
    /// Сохранённые логи k8s: файл или каталог выгрузки. Обязательно: база уведомлений восстановлена
    /// из бэкапа 2026-10-06 ~22:45 UTC, и проекты за последние сутки до аварии есть только в логах.
    /// Путь на машине админа — в user-secrets или DRP_*, в drp.appsettings.json пусто.
    /// </summary>
    public const string LogsKey = "Drp:K8sLogs";

    private static readonly string[] LogExtensions = [".json", ".ndjson", ".jsonl"];

    public static readonly IReadOnlyDictionary<string, string> CommandLineOptions = new Dictionary<string, string>
    {
        [BackupMaxOption] = DrpKeys.BackupMax("Projects"),
        [LogsOption] = LogsKey,
    };

    public static readonly IReadOnlyCollection<string> CommandLineFlags = [];

    public static string Usage => $"""
        Составляет список проектов, созданных после бэкапа основной БД, по базе уведомлений и логам.
        Ничего не пишет ни в одну базу — только CSV-отчёт для ручной проверки.

        Логи k8s обязательны ({LogsKey}): проекты за последние сутки до аварии есть только в них.
        Задайте путь в user-secrets: dotnet user-secrets set --id {DrpConfiguration.UserSecretsId} "{LogsKey}" "<путь>"
        или переменной {DrpConfiguration.EnvironmentPrefix}{LogsKey.Replace(":", "__")}.

        Параметры (все необязательны, переопределяют настройки):
          --lost-at <RFC 3339>              момент аварии (Drp:LostAt), например 2026-10-07T22:30:49Z
          {BackupMaxOption} <число>   максимальный ProjectId бэкапа (Drp:BackupMax:Projects)
          {LogsOption} <путь>                     логи k8s ({LogsKey}): файл (лучше k8s-prod-mutations-merged.json) или каталог
                                            с файлами {string.Join(", ", LogExtensions)}; повторы записей по uid отбрасываются
                                            (yc logging read --format json — массив или объект на строку)
          --report <путь>                   записать отчёт сюда, а не в Drp:ReportDir

        {DrpConfiguration.SourcesHelp}
        """;

    /// <summary>
    /// Собирает параметры из конфигурации. Если чего-то не хватает, бросает
    /// <see cref="DrpConfigurationException"/> со всеми ошибками сразу.
    /// </summary>
    public static Options FromConfiguration(IConfiguration configuration, DateTimeOffset startedAt)
    {
        var settings = new DrpSettings(configuration);
        var mssql = settings.ConnectionString(DrpKeys.MainConnectionName, "основная БД, MSSQL");
        var notifications = settings.ConnectionString(DrpKeys.NotificationsConnectionName, "база уведомлений, Postgres");
        var backupMax = settings.BackupMax("Projects", BackupMaxOption);
        var lostAt = settings.LostAt();
        var (logPaths, logsError) = ResolveLogPaths(configuration[LogsKey]);
        var reportPath = settings.ReportPath(ToolName, startedAt);

        var errors = settings.Errors.ToList();
        if (logsError is not null)
        {
            errors.Add(logsError);
        }
        if (errors.Count > 0)
        {
            throw new DrpConfigurationException(errors);
        }
        return new Options(mssql, notifications, backupMax, lostAt, reportPath, logPaths);
    }

    private static (IReadOnlyList<string> Paths, string? Error) ResolveLogPaths(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ([],
                $"Не заданы логи k8s {LogsKey}: база уведомлений восстановлена из бэкапа 2026-10-06, и проекты за последние сутки до аварии есть только в логах. "
                + $"Задайте путь: dotnet user-secrets set --id {DrpConfiguration.UserSecretsId} \"{LogsKey}\" \"<путь>\" "
                + $"или переменной окружения {DrpConfiguration.EnvironmentPrefix}{LogsKey.Replace(":", "__")} (или передайте {LogsOption}).");
        }
        if (File.Exists(value))
        {
            return ([Path.GetFullPath(value)], null);
        }
        if (Directory.Exists(value))
        {
            var files = Directory.EnumerateFiles(value, "*", SearchOption.AllDirectories)
                .Where(f => LogExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                .Select(Path.GetFullPath)
                .Order(StringComparer.Ordinal)
                .ToList();
            return files.Count > 0
                ? (files, null)
                : ([], $"В каталоге логов {LogsKey} = «{value}» нет файлов {string.Join(", ", LogExtensions)}.");
        }
        return ([], $"Не найден файл или каталог логов {LogsKey} = «{value}».");
    }
}
