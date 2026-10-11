using System.Globalization;
using JoinRpg.Tools.Drp.Common;

namespace JoinRpg.Tools.RestoreProjectShells;

/// <summary>
/// DRP-инструмент (план #5459, фаза 4, #5491): по проверенному списку проектов, созданных после бэкапа,
/// создаёт их оболочки с прежними id, чтобы было куда восстанавливать персонажей и заявки.
/// По умолчанию только пишет отчёт; с --apply создаёт оболочки.
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
        IReadOnlyList<LostProjectsCsvRow> rows;
        try
        {
            options = Options.FromConfiguration(DrpConfiguration.Build(commandLine), commandLine.HasFlag(Options.ApplyFlag), DateTimeOffset.UtcNow);
            rows = await LostProjectsCsv.Read(options.InputPath, CancellationToken.None);
        }
        catch (DrpConfigurationException e)
        {
            return PrintErrors(e.Errors);
        }
        catch (DrpCsvException e)
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

        Console.WriteLine(options.Apply ? "Режим: создание оболочек (--apply)." : "Режим: только отчёт (dry-run).");
        Console.WriteLine("Запускать при остановленных Portal и IdPortal.");

        var mainDb = new MainDatabase(options.MssqlConnectionString);
        var state = await mainDb.GetState(options.BackupMaxProjectId, ProjectShellPlanner.CreatorIdsToLookUp(rows), ct);
        var identityBefore = await mainDb.GetProjectsIdentityCurrent(ct);
        Console.WriteLine($"Список: строк {rows.Count}, из них «{LostProjectsCsv.Yes}» — {rows.Count(r => r.Create)}.");
        Console.WriteLine($"Максимальный ProjectId бэкапа ({DrpKeys.BackupMax("Projects")}): {options.BackupMaxProjectId}.");
        Console.WriteLine($"Счётчик Projects: IDENT_CURRENT {Format(identityBefore)}, в drp.ReseedLog {Format(state.ShiftedProjectsSeed)}.");

        var decisions = new ProjectShellPlanner(options.BackupMaxProjectId, state).Plan(rows);
        var results = new Dictionary<int, string>();
        await ReportWriter.Write(options.ReportPath, decisions, results, ct);
        Console.WriteLine($"Отчёт записан: {options.ReportPath}.");
        PrintSummary(decisions);

        var toCreate = decisions.Where(d => d.Kind == ShellDecisionKind.Create).ToList();
        if (!options.Apply)
        {
            Console.WriteLine("Dry-run: в MSSQL ничего не записано. Для создания оболочек запустите с --apply.");
            return 0;
        }
        if (toCreate.Count == 0)
        {
            Console.WriteLine("Создавать нечего.");
            return 0;
        }

        var exitCode = 0;
        try
        {
            await using var host = new DomainHost(options.MssqlConnectionString);
            foreach (var shell in toCreate)
            {
                ct.ThrowIfCancellationRequested();
                await mainDb.ReseedProjectsBefore(shell.ProjectId, ct);
                int createdId;
                try
                {
                    createdId = await host.CreateShell(shell.CreatorUserId!.Value, shell.Name!);
                }
                catch (Exception e)
                {
                    results[shell.ProjectId] = "ошибка: " + e.GetBaseException().Message;
                    Console.Error.WriteLine($"Проект {shell.ProjectId}: ошибка создания ({e.GetType().Name}), подробности в отчёте. Остановка.");
                    exitCode = 1;
                    break;
                }

                if (createdId != shell.ProjectId)
                {
                    results[shell.ProjectId] = $"ошибка: проект получил id {createdId.ToString(CultureInfo.InvariantCulture)}";
                    Console.Error.WriteLine($"Проект {shell.ProjectId}: получил id {createdId}, а не прежний. Остановка: разберитесь с проектом {createdId} вручную.");
                    exitCode = 1;
                    break;
                }
                results[shell.ProjectId] = "создан";
                Console.WriteLine($"  создан {shell.ProjectId}");
            }
        }
        finally
        {
            await ReportWriter.Write(options.ReportPath, decisions, results, CancellationToken.None);
            var created = results.Count(r => r.Value == "создан");
            Console.WriteLine($"Создано оболочек: {created} из {toCreate.Count}. Отчёт обновлён: {options.ReportPath}.");
            Console.WriteLine($"IDENT_CURRENT('dbo.Projects') сейчас {Format(await mainDb.GetProjectsIdentityCurrent(CancellationToken.None))}: счётчик опущен.");
            Console.WriteLine("ОБЯЗАТЕЛЬНО до запуска Portal и IdPortal прогоните reseed-identities.sql с @Apply = 1 — он вернёт счётчик Projects (действие Restore).");
        }
        return exitCode;
    }

    private static string Format(long? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "нет";

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

    private static void PrintSummary(IReadOnlyList<ShellDecision> decisions)
    {
        Console.WriteLine();
        foreach (var group in decisions.GroupBy(d => d.Kind).OrderBy(g => g.Key))
        {
            var ids = string.Join(", ", group.Select(d => d.ProjectId.ToString(CultureInfo.InvariantCulture)));
            Console.WriteLine($"  {group.Key} ({group.First().Reason}): {group.Count()} — {ids}");
        }
        Console.WriteLine();
    }
}
