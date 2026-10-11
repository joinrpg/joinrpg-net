using System.Globalization;
using JoinRpg.Tools.Drp.Common;

namespace JoinRpg.Tools.RestoreLostProjects;

/// <summary>
/// DRP-инструмент (план #5459, фаза 4): после восстановления основной БД из старого бэкапа составляет
/// список проектов, созданных после бэкапа, — для ручной проверки и создания оболочек с прежними id.
/// Источники — база уведомлений (Postgres), пережившая потерю MSSQL, и сохранённые логи k8s.
/// Только читает.
/// </summary>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var (commandLine, error) = DrpCommandLine.Parse(args, Options.CommandLineOptions, Options.CommandLineFlags);
        if (commandLine is null)
        {
            return PrintErrors([error!]);
        }

        Options options;
        try
        {
            options = Options.FromConfiguration(DrpConfiguration.Build(commandLine), DateTimeOffset.UtcNow);
        }
        catch (DrpConfigurationException e)
        {
            return PrintErrors(e.Errors);
        }

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };
        var ct = cts.Token;

        var mainDb = new MainDatabase(options.MssqlConnectionString);
        var notifications = new NotificationsSource(options.NotificationsConnectionString, options.LostAt);

        Console.WriteLine($"Момент аварии ({DrpKeys.LostAt}):{options.LostAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} UTC; более поздние уведомления и записи логов не учитываются.");

        var projects = await mainDb.GetProjects(ct);
        var users = await mainDb.GetUsers(ct);
        var currentMax = projects.Count == 0 ? 0 : projects.Keys.Max();
        Console.WriteLine($"MSSQL: проектов {projects.Count}, максимальный ProjectId {currentMax}; пользователей {users.Count}.");
        Console.WriteLine($"Максимальный ProjectId бэкапа ({DrpKeys.BackupMax("Projects")}): {options.BackupMaxProjectId}.");

        var virtualUserIds = users
            .Where(u => MainDatabase.VirtualUserEmails.Contains(u.Email, StringComparer.OrdinalIgnoreCase))
            .Select(u => u.UserId)
            .ToHashSet();

        // MSSQL знает админов на момент бэкапа, уведомления админам — назначенных позже (с 2026-08-23).
        var mssqlAdmins = await mainDb.GetAdminUserIds(ct);
        var notifiedAdmins = await notifications.GetAdminUserIds(ct);
        var adminUserIds = mssqlAdmins.Union(notifiedAdmins).ToHashSet();
        Console.WriteLine($"Админы: в MSSQL {mssqlAdmins.Count}, получателей уведомлений админам {notifiedAdmins.Count}, всего {adminUserIds.Count}.");

        var analyzer = new LostProjectAnalyzer(projects, users, virtualUserIds, adminUserIds, options.BackupMaxProjectId);

        var allReferences = await notifications.GetEntityReferences(ct);
        var references = analyzer.SelectReferences(allReferences);
        Console.WriteLine($"Уведомления: различных EntityReference {allReferences.Count}, из них по интересным проектам {references.Count}.");

        var rows = await notifications.GetNotifications(references, ct);
        Console.WriteLine($"Уведомления: строк по интересным проектам {rows.Count}.");

        var logs = await LogsSource.Read(options.LogPaths, options.LostAt, ct);
        Console.WriteLine($"Логи: файлов {options.LogPaths.Count}, записей с проектом или путём (без повторов по uid) {logs.Count}.");

        var report = analyzer.Analyze(rows, logs);

        await ReportWriter.Write(options.ReportPath, report, ct);
        Console.WriteLine($"Отчёт записан: {options.ReportPath} (содержит персональные данные, не публиковать).");

        PrintSummary(report);
        return 0;
    }

    private static int PrintErrors(IEnumerable<string> errors)
    {
        foreach (var error in errors)
        {
            Console.Error.WriteLine(error);
        }
        Console.Error.WriteLine();
        Console.Error.WriteLine(Options.Usage);
        return 2;
    }

    private static void PrintSummary(IReadOnlyList<LostProjectReport> report)
    {
        int Count(Func<LostProjectReport, bool> predicate) => report.Count(predicate);
        bool Has(LostProjectReport p, ProjectFlags flag) => p.Flags.HasFlag(flag);

        Console.WriteLine();
        Console.WriteLine($"Проектов в отчёте: {report.Count}.");
        Console.WriteLine($"  id выше майского максимума: {Count(p => !Has(p, ProjectFlags.IdNotAboveBackupMax))}, из них уже есть в MSSQL: {Count(p => Has(p, ProjectFlags.AlreadyInMssql))}");
        Console.WriteLine($"  противоречие (id не выше максимума, но проекта нет в MSSQL): {Count(p => Has(p, ProjectFlags.IdNotAboveBackupMax))}");
        Console.WriteLine($"  название: найдено {Count(p => p.Name is not null)}, конфликт {Count(p => Has(p, ProjectFlags.NameConflict))}");
        Console.WriteLine($"  создатель: точно {Count(p => p.CreatorMethod == CreatorMethod.Exact)}, предположительно {Count(p => p.CreatorMethod == CreatorMethod.Presumed)}, по логам {Count(p => p.CreatorMethod == CreatorMethod.PresumedFromLogs)}, не найден {Count(p => p.CreatorMethod == CreatorMethod.NotFound)}, нет в MSSQL {Count(p => Has(p, ProjectFlags.CreatorMissingInMssql))}");
        Console.WriteLine($"  только в логах (без уведомлений): {Count(p => Has(p, ProjectFlags.OnlyInLogs))}");
        Console.WriteLine($"  с привязкой к КогдаИгре: {Count(p => p.KogdaIgra is not null)}");
        Console.WriteLine();
    }
}
