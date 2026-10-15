using System.Globalization;
using JoinRpg.Services.Drp.ManualFinance;
using JoinRpg.Tools.Drp.Common;

namespace JoinRpg.Tools.RestoreManualFinance;

/// <summary>
/// DRP-инструмент (план #5459, фаза 9, #5561): возвращает в заявки заблокированных проектов ручные оплаты и
/// возвраты взноса и решения по ним, которые после бэкапа остались только в уведомлениях.
/// По умолчанию только пишет CSV с планом; с --apply --input восстанавливает по проверенному CSV.
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
        IReadOnlyList<ManualFinanceCsvRow> csvRows = [];
        try
        {
            options = Options.FromConfiguration(DrpConfiguration.Build(commandLine), commandLine.HasFlag(Options.ApplyFlag), DateTimeOffset.UtcNow);
            if (options.InputPath is { } input)
            {
                csvRows = await ManualFinanceCsv.Read(input, CancellationToken.None);
            }
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

        Console.WriteLine(options.Apply ? $"Режим: восстановление по {options.InputPath} (--apply)." : "Режим: только отчёт (dry-run).");
        Console.WriteLine("Portal останавливать не нужно: счётчики id инструмент не трогает.");
        Console.WriteLine($"Уведомления до {Format(options.LostAt)}, восстанавливается всё после бэкапа {Format(options.BackupTakenAt)}.");

        await using var host = new DomainHost(options.MssqlConnectionString, options.NotificationsConnectionString);
        var projectIds = await host.GetBlockedProjects();
        Console.WriteLine($"Заблокированных проектов: {projectIds.Count}.");

        var plans = new List<ProjectFinancePlan>();
        foreach (var projectId in projectIds)
        {
            ct.ThrowIfCancellationRequested();
            plans.Add(await host.LoadPlan(projectId, options.LostAt, options.BackupTakenAt));
        }

        var results = new ApplyResults();
        await ReportWriter.Write(options.ReportPath, plans, results, ct);
        Console.WriteLine($"Отчёт записан: {options.ReportPath}.");
        PrintSummary(plans);

        if (!options.Apply)
        {
            Console.WriteLine($"Dry-run: в базу ничего не записано. Проверьте колонку «{ManualFinanceCsv.PaymentType}» и запустите с {Options.ApplyFlag} {Options.InputOption} <CSV>.");
            return 0;
        }

        var planned = projectIds.ToHashSet();
        foreach (var row in csvRows.Where(r => !planned.Contains(r.ProjectId)))
        {
            Console.WriteLine($"  Строка {row.Line}: проект {row.ProjectId.Value} не заблокирован или не найден — строка не применяется.");
            if (row.PaymentType is not null)
            {
                results.UnappliedCsvLines.Add(row.Line);
            }
        }

        try
        {
            var applier = new ManualFinanceApplier(host, Console.Out, Console.Error);
            foreach (var plan in plans)
            {
                ct.ThrowIfCancellationRequested();
                if (plan.ActingMaster is { } master && plan.Rows.Count > 0)
                {
                    Console.WriteLine($"Проект {plan.ProjectId.Value}: от имени мастера {master.Value}.");
                }
                await applier.Apply(plan, csvRows, results, ct);
            }
        }
        finally
        {
            await ReportWriter.Write(options.ReportPath, plans, results, CancellationToken.None);
            var outcomes = results.Entries.Values.ToList();
            Console.WriteLine();
            Console.WriteLine($"Строк плана с результатом: {outcomes.Count}, из них вставлено {outcomes.Count(o => o.Text.StartsWith("вставлено", StringComparison.Ordinal))}, ошибок {outcomes.Count(o => o.IsError)}.");
            if (results.UnappliedCsvLines.Count > 0)
            {
                Console.Error.WriteLine(
                    $"Строк CSV с заполненным «{ManualFinanceCsv.PaymentType}», которые не применились (проект не заблокирован или ключа нет в плане): "
                    + $"{results.UnappliedCsvLines.Count} — строки {string.Join(", ", results.UnappliedCsvLines.Order())}.");
            }
            Console.WriteLine($"Отчёт обновлён: {options.ReportPath}.");
        }
        return results.HasErrors ? 1 : 0;
    }

    private static string Format(DateTimeOffset value) => value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture);

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

    private static void PrintSummary(IReadOnlyList<ProjectFinancePlan> plans)
    {
        Console.WriteLine();
        foreach (var plan in plans.Where(p => p.Rows.Count > 0))
        {
            var operations = plan.Rows.SelectMany(r => r.Operations).ToList();
            var moderations = plan.Rows.SelectMany(r => r.Moderations).ToList();
            var toRestore = operations.Where(o => o.Status == FinanceRestoreStatus.Restore).ToList();
            Console.WriteLine(
                $"  Проект {plan.ProjectId.Value}: заявок {plan.Rows.Count} (нет в базе {plan.Rows.Count(r => !r.ClaimExists)}), "
                + $"операций {operations.Count}: восстановить {toRestore.Count} "
                + $"(тип подобран {toRestore.Count(o => o.PaymentType.PaymentTypeId is not null)}, "
                + $"создать наличные {toRestore.Count(o => o.PaymentType.How == PaymentTypeGuess.CreateCash)}, "
                + $"не определён {toRestore.Count(o => o.PaymentType.How is PaymentTypeGuess.Ambiguous or PaymentTypeGuess.NotFound)}), "
                + $"уже есть {operations.Count(o => o.Status == FinanceRestoreStatus.AlreadyExists)}, "
                + $"конфликтов {operations.Count(o => o.Status == FinanceRestoreStatus.Conflict)}, "
                + $"с некорректной суммой {operations.Count(o => o.Status == FinanceRestoreStatus.InvalidMoney)}; "
                + $"решений {moderations.Count}: восстановить {moderations.Count(m => m.Status == FinanceRestoreStatus.Restore)}.");
        }
        Console.WriteLine($"  Проектов без ручных финансовых событий после бэкапа: {plans.Count(p => p.Rows.Count == 0)}.");
        Console.WriteLine();
    }
}
