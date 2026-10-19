using JoinRpg.Tools.Drp.Common;
using Microsoft.Extensions.Configuration;

namespace JoinRpg.Tools.RestoreLostUsers;

internal record Options(
    string MssqlConnectionString,
    string NotificationsConnectionString,
    int BackupMaxUserId,
    DateTimeOffset LostAt,
    string ReportPath,
    bool Apply)
{
    public const string ToolName = "restore-lost-users";
    public const string ApplyFlag = "--apply";
    public const string BackupMaxOption = "--backup-max-user-id";

    public static readonly IReadOnlyDictionary<string, string> CommandLineOptions = new Dictionary<string, string>
    {
        [BackupMaxOption] = DrpKeys.BackupMax("Users"),
    };

    public static readonly IReadOnlyCollection<string> CommandLineFlags = [ApplyFlag];

    public static string Usage => $"""
        Восстанавливает заготовки пользователей, потерянных вместе с основной БД, по базе уведомлений.

        Запускать при остановленных Portal и IdPortal: --apply держит блокировку на таблице Users
        до конца транзакции, а регистрации во время работы могут занять потерянные id.

        Параметры (все необязательны):
          {ApplyFlag}                        создать пользователей; без этого флага — только отчёт (dry-run)
          --lost-at <RFC 3339>           переопределить момент аварии (Drp:LostAt), например 2026-10-07T22:30:49Z
          {BackupMaxOption} <число>   переопределить максимальный UserId бэкапа (Drp:BackupMax:Users)
          --report <путь>                записать отчёт сюда, а не в Drp:ReportDir

        {DrpConfiguration.SourcesHelp}
        """;

    /// <summary>
    /// Собирает параметры из конфигурации. Если чего-то не хватает, бросает
    /// <see cref="DrpConfigurationException"/> со всеми ошибками сразу.
    /// </summary>
    public static Options FromConfiguration(IConfiguration configuration, bool apply, DateTimeOffset startedAt)
    {
        var settings = new DrpSettings(configuration);
        var options = new Options(
            settings.ConnectionString(DrpKeys.MainConnectionName, "основная БД, MSSQL"),
            settings.ConnectionString(DrpKeys.NotificationsConnectionName, "база уведомлений, Postgres"),
            settings.BackupMax("Users", BackupMaxOption),
            settings.LostAt(),
            settings.ReportPath(ToolName, startedAt),
            apply);
        settings.ThrowIfErrors();
        return options;
    }
}
