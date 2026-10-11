using System.Globalization;
using JoinRpg.Tools.Drp.Common;

namespace JoinRpg.Tools.RestoreLostUsers;

/// <summary>
/// DRP-инструмент (план #5459, фаза 2): после восстановления основной БД из старого бэкапа создаёт
/// заготовки пользователей, зарегистрированных после бэкапа, с их прежними id. Источник — база
/// уведомлений (Postgres), которая пережила потерю MSSQL. По умолчанию только пишет отчёт;
/// с --apply создаёт пользователей.
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
            options = Options.FromConfiguration(DrpConfiguration.Build(commandLine), commandLine.HasFlag(Options.ApplyFlag), DateTimeOffset.UtcNow);
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

        Console.WriteLine(options.Apply ? "Режим: создание пользователей (--apply)." : "Режим: только отчёт (dry-run).");
        Console.WriteLine("Запускать при остановленных Portal и IdPortal.");
        Console.WriteLine($"Момент аварии ({DrpKeys.LostAt}): {options.LostAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} UTC; более поздние уведомления не учитываются.");

        var existingUsers = await mainDb.GetUsers(ct);
        var identityBefore = await mainDb.GetIdentityCurrent(ct);
        var currentMax = existingUsers.Count == 0 ? 0 : existingUsers.Max(u => u.UserId);
        Console.WriteLine($"MSSQL: пользователей {existingUsers.Count}, максимальный UserId {currentMax}, IDENT_CURRENT {identityBefore?.ToString(CultureInfo.InvariantCulture) ?? "нет"}.");
        Console.WriteLine($"Максимальный UserId бэкапа ({DrpKeys.BackupMax("Users")}): {options.BackupMaxUserId}.");

        var systemUserIds = existingUsers
            .Where(u => MainDatabase.VirtualUserEmails.Contains(u.Email, StringComparer.OrdinalIgnoreCase))
            .Select(u => u.UserId)
            .ToHashSet();
        if (systemUserIds.Count != MainDatabase.VirtualUserEmails.Length)
        {
            Console.WriteLine("Внимание: в MSSQL найдены не все виртуальные пользователи (робот, платежи).");
        }

        var activity = await notifications.GetActivity(ct);
        Console.WriteLine($"Уведомления: различных id пользователей {activity.Count}.");

        var planner = new LostUserPlanner(existingUsers, systemUserIds, options.BackupMaxUserId, options.LostAt);
        var details = await notifications.GetDetails(planner.SelectIdsNeedingDetails(activity), ct);
        var decisions = planner.Plan(activity, details);

        await ReportWriter.Write(options.ReportPath, decisions, ct);
        Console.WriteLine($"Отчёт записан: {options.ReportPath} (содержит персональные данные, не публиковать).");

        PrintSummary(decisions);

        var toCreate = decisions.Where(d => d.Kind == DecisionKind.Create).ToList();
        // Следующая регистрация получит IDENT_CURRENT + 1, так что под угрозой только id строго выше счётчика.
        if (toCreate.Count > 0 && identityBefore is decimal identity && toCreate.Max(d => d.UserId) > identity)
        {
            Console.WriteLine(
                "Внимание: IDENT_CURRENT ниже потерянных id — новые регистрации могут занять их. Сдвиньте счётчик: DBCC CHECKIDENT ('dbo.Users', RESEED, "
                + toCreate.Max(d => d.UserId).ToString(CultureInfo.InvariantCulture) + ") или выше.");
        }

        if (!options.Apply)
        {
            Console.WriteLine("Dry-run: в MSSQL ничего не записано. Для создания пользователей запустите с --apply.");
            return 0;
        }

        if (toCreate.Count == 0)
        {
            Console.WriteLine("Создавать некого.");
            return 0;
        }

        Console.WriteLine("Portal и IdPortal должны быть остановлены: таблица Users заблокирована до конца транзакции.");
        var created = await mainDb.CreateUsers([.. toCreate.Select(d => NewUserRow.From(d, Guid.NewGuid()))], ct);
        Console.WriteLine($"Создано пользователей: {created.Count} из {toCreate.Count}.");
        var skipped = toCreate.Select(d => d.UserId).Except(created).Order().ToList();
        if (skipped.Count > 0)
        {
            Console.WriteLine(
                $"Пропущено, потому что id или адрес почты оказались заняты к моменту записи: {skipped.Count}. Id: "
                + string.Join(", ", skipped.Select(id => id.ToString(CultureInfo.InvariantCulture))));
        }

        var identityAfter = await mainDb.GetIdentityCurrent(ct);
        Console.WriteLine($"IDENT_CURRENT после записи: {identityAfter?.ToString(CultureInfo.InvariantCulture) ?? "нет"}.");
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

    private static void PrintSummary(IReadOnlyList<LostUserDecision> decisions)
    {
        Console.WriteLine();
        Console.WriteLine("Сводка по пользователям из уведомлений, которых нет в MSSQL или чей id выше максимума бэкапа:");
        foreach (var group in decisions.GroupBy(d => d.Kind).OrderBy(g => g.Key))
        {
            var sample = group.First();
            var range = $"id {group.Min(d => d.UserId)}–{group.Max(d => d.UserId)}";
            Console.WriteLine($"  {group.Key}: {group.Count()} ({range}) — {sample.Reason}");
        }
        var toCreate = decisions.Where(d => d.Kind == DecisionKind.Create).ToList();
        if (toCreate.Count > 0)
        {
            Console.WriteLine($"  из них с именем из приветствий: {toCreate.Count(d => d.Name is not null)}, с Telegram: {toCreate.Count(d => d.TelegramChatId is not null)} (Telegram не восстанавливается, только в отчёте)");
        }
        Console.WriteLine();
    }
}
