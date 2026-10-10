using System.Globalization;

namespace JoinRpg.Tools.RestoreLostProjects;

internal record Options(
    string MssqlConnectionString,
    string NotificationsConnectionString,
    int BackupMaxProjectId,
    DateTimeOffset LostAt,
    string ReportPath,
    IReadOnlyList<string> LogPaths)
{
    // Те же переменные, что у RestoreLostUsers: инструменты запускаются подряд с одной машины.
    public const string MssqlEnvironmentVariable = "RESTORE_MSSQL";
    public const string NotificationsEnvironmentVariable = "RESTORE_NOTIFICATIONS";

    public const string Usage = $"""
        Составляет список проектов, созданных после бэкапа основной БД, по базе уведомлений и логам.
        Ничего не пишет ни в одну базу — только CSV-отчёт для ручной проверки.

        Параметры:
          --mssql <строка>                  строка подключения к основной БД (MSSQL), или переменная {MssqlEnvironmentVariable}
          --notifications <строка>          строка подключения к базе уведомлений (Postgres), или переменная {NotificationsEnvironmentVariable}
          --backup-max-project-id <число>   максимальный ProjectId в восстановленном бэкапе (SELECT MAX(ProjectId) FROM Projects
                                            сразу после восстановления; после создания оболочек он вырастет)
          --lost-at <RFC 3339>              момент аварии, например 2026-10-07T22:30:49Z; более поздние уведомления не учитываются
          --report <путь>                   куда записать CSV-отчёт (содержит персональные данные)
          --logs <путь>                     необязательно, можно несколько раз: сохранённые логи k8s
                                            (yc logging read --format json — массив или объект на строку)
        """;

    // Только с явным часовым поясом: момент без пояса молча истолковался бы по часам машины.
    private static readonly string[] LostAtFormats =
    [
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz",
    ];

    /// <summary>
    /// Разбирает аргументы командной строки. Строки подключения можно передать через переменные окружения,
    /// чтобы пароли не попадали в историю команд.
    /// </summary>
    /// <returns>Параметры или текст ошибки.</returns>
    public static (Options? Options, string? Error) Parse(IReadOnlyList<string> args, Func<string, string?> getEnvironmentVariable)
    {
        string? mssql = null, notifications = null, backupMax = null, lostAtText = null, report = null;
        var logs = new List<string>();

        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--mssql" or "--notifications" or "--backup-max-project-id" or "--lost-at" or "--report" or "--logs":
                    if (i + 1 >= args.Count)
                    {
                        return (null, $"Не задано значение параметра {args[i]}");
                    }
                    var value = args[++i];
                    switch (args[i - 1])
                    {
                        case "--mssql": mssql = value; break;
                        case "--notifications": notifications = value; break;
                        case "--backup-max-project-id": backupMax = value; break;
                        case "--lost-at": lostAtText = value; break;
                        case "--report": report = value; break;
                        case "--logs": logs.Add(value); break;
                    }
                    break;
                default:
                    return (null, $"Неизвестный параметр {args[i]}");
            }
        }

        mssql ??= getEnvironmentVariable(MssqlEnvironmentVariable);
        notifications ??= getEnvironmentVariable(NotificationsEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(mssql))
        {
            return (null, "Не задана строка подключения к MSSQL (--mssql)");
        }
        if (string.IsNullOrWhiteSpace(notifications))
        {
            return (null, "Не задана строка подключения к базе уведомлений (--notifications)");
        }
        if (!int.TryParse(backupMax, NumberStyles.None, CultureInfo.InvariantCulture, out var backupMaxProjectId) || backupMaxProjectId <= 0)
        {
            return (null, "Не задан или некорректен --backup-max-project-id");
        }
        if (!DateTimeOffset.TryParseExact(lostAtText, LostAtFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var lostAt))
        {
            return (null, "Не задан или некорректен --lost-at (нужен момент с часовым поясом, например 2026-10-07T22:30:49Z)");
        }
        if (string.IsNullOrWhiteSpace(report))
        {
            return (null, "Не задан путь к отчёту (--report)");
        }

        return (new Options(mssql, notifications, backupMaxProjectId, lostAt, report, logs), null);
    }
}
