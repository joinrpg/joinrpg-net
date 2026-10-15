using JoinRpg.Tools.Drp.Common;

namespace JoinRpg.Tools.RestoreManualFinance;

/// <param name="InputPath">Проверенный CSV — только при <paramref name="Apply"/>.</param>
internal record Options(
    string MssqlConnectionString,
    string NotificationsConnectionString,
    DateTimeOffset LostAt,
    DateTimeOffset BackupTakenAt,
    string ReportPath,
    bool Apply,
    string? InputPath)
{
    public const string ToolName = "restore-manual-finance";
    public const string ApplyFlag = "--apply";
    public const string InputOption = "--input";
    public const string BackupTakenAtOption = "--backup-taken-at";

    /// <summary>Проверенный CSV прошлого dry-run. Только разовое значение: файл свой у каждого прогона.</summary>
    public const string InputKey = "Drp:ManualFinance:Input";

    public static readonly IReadOnlyDictionary<string, string> CommandLineOptions = new Dictionary<string, string>
    {
        [InputOption] = InputKey,
        [BackupTakenAtOption] = DrpKeys.BackupTakenAt,
    };

    public static readonly IReadOnlyCollection<string> CommandLineFlags = [ApplyFlag];

    public static string Usage => $"""
        Восстанавливает ручные оплаты и возвраты взноса (типы оплаты «наличные» и «другое») и решения
        мастеров по ним, которые после бэкапа остались только в уведомлениях (DRP, #5561), — во всех
        заблокированных проектах, с прежними id, без уведомлений.

        Без {ApplyFlag} — только CSV-отчёт с планом (dry-run). В отчёте проверьте колонку «{ManualFinanceCsv.PaymentType}»:
          <id типа оплаты>   тип проекта, на который восстановить операцию (предзаполнен, если подобран однозначно);
          cash:<userId>      создать (или взять имеющийся) тип «наличные» этого мастера и восстановить на него;
          пусто              не восстанавливать операцию в этот раз.
        Затем запустите с {ApplyFlag} {InputOption} <проверенный CSV>: план строится заново, из CSV берётся только
        тип оплаты. Повторный запуск по тому же CSV ничего не меняет.

        Portal и IdPortal останавливать не нужно: счётчики id инструмент не трогает.

        Параметры:
          {ApplyFlag}                       применить; без этого флага — только отчёт (dry-run)
          {InputOption} <путь>                проверенный CSV прошлого dry-run (обязателен с {ApplyFlag})
          {BackupTakenAtOption} <момент>    переопределить момент бэкапа ({DrpKeys.BackupTakenAt})
          --lost-at <момент>            переопределить момент аварии ({DrpKeys.LostAt})
          --report <путь>               записать отчёт сюда, а не в {DrpKeys.ReportDir}

        {DrpConfiguration.SourcesHelp}
        """;

    /// <summary>
    /// Собирает параметры из конфигурации. Если чего-то не хватает, бросает
    /// <see cref="DrpConfigurationException"/> со всеми ошибками сразу.
    /// </summary>
    public static Options FromConfiguration(IConfiguration configuration, bool apply, DateTimeOffset startedAt)
    {
        var settings = new DrpSettings(configuration);
        var mssql = settings.ConnectionString(DrpKeys.MainConnectionName, "основная БД, MSSQL");
        var notifications = settings.ConnectionString(DrpKeys.NotificationsConnectionName, "база уведомлений, Postgres");
        var lostAt = settings.LostAt();
        var backupTakenAt = settings.BackupTakenAt(BackupTakenAtOption);
        var reportPath = settings.ReportPath(ToolName, startedAt);

        var errors = settings.Errors.ToList();
        if (errors.Count == 0 && backupTakenAt >= lostAt)
        {
            errors.Add($"Момент бэкапа {DrpKeys.BackupTakenAt} должен быть раньше момента аварии {DrpKeys.LostAt}.");
        }

        var input = configuration[InputKey];
        string? inputPath = null;
        if (apply)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                errors.Add($"С {ApplyFlag} нужен проверенный CSV: передайте {InputOption} <путь к отчёту dry-run>.");
            }
            else if (!File.Exists(input))
            {
                errors.Add($"Не найден файл «{input}».");
            }
            else
            {
                inputPath = Path.GetFullPath(input);
            }
        }
        else if (!string.IsNullOrWhiteSpace(input))
        {
            errors.Add($"{InputOption} читается только с {ApplyFlag}: dry-run строит план без CSV.");
        }

        if (errors.Count > 0)
        {
            throw new DrpConfigurationException(errors);
        }
        return new Options(mssql, notifications, lostAt, backupTakenAt, reportPath, apply, inputPath);
    }
}
