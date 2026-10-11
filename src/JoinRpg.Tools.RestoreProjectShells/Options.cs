using JoinRpg.Tools.Drp.Common;

namespace JoinRpg.Tools.RestoreProjectShells;

internal record Options(
    string MssqlConnectionString,
    int BackupMaxProjectId,
    string InputPath,
    string ReportPath,
    bool Apply)
{
    public const string ToolName = "restore-project-shells";
    public const string ApplyFlag = "--apply";
    public const string BackupMaxOption = "--backup-max-project-id";
    public const string InputOption = "--input";

    /// <summary>
    /// Проверенный список из RestoreLostProjects (#5479). Только разовое значение: файл свой у каждого прогона.
    /// </summary>
    public const string InputKey = "Drp:ProjectShells:Input";

    public static readonly IReadOnlyDictionary<string, string> CommandLineOptions = new Dictionary<string, string>
    {
        [BackupMaxOption] = DrpKeys.BackupMax("Projects"),
        [InputOption] = InputKey,
    };

    public static readonly IReadOnlyCollection<string> CommandLineFlags = [ApplyFlag];

    public static string Usage => $"""
        Создаёт оболочки проектов, созданных после бэкапа, с прежними id — по списку RestoreLostProjects,
        проверенному вручную (колонка «{LostProjectsCsv.Create}»). Оболочка — проект, корневая группа и права
        создателя, в статусе «Заблокирован», без уведомлений.

        Запускать при остановленных Portal и IdPortal: инструмент опускает счётчик dbo.Projects.
        Перед запуском: сдвиг счётчиков reseed-identities.sql, заготовки пользователей (RestoreLostUsers).
        После — ещё раз reseed-identities.sql с @Apply = 1: он вернёт счётчик Projects.

        Параметры:
          {InputOption} <путь>                     проверенный CSV из RestoreLostProjects (обязательно)
          {ApplyFlag}                             создать оболочки; без этого флага — только отчёт (dry-run)
          {BackupMaxOption} <число>   переопределить максимальный ProjectId бэкапа (Drp:BackupMax:Projects)
          --report <путь>                     записать отчёт сюда, а не в Drp:ReportDir

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
        var backupMax = settings.BackupMax("Projects", BackupMaxOption);
        var reportPath = settings.ReportPath(ToolName, startedAt);

        var errors = settings.Errors.ToList();
        var input = configuration[InputKey];
        if (string.IsNullOrWhiteSpace(input))
        {
            errors.Add($"Не задан проверенный список проектов: передайте {InputOption} <путь к CSV из RestoreLostProjects>.");
        }
        else if (!File.Exists(input))
        {
            errors.Add($"Не найден файл списка проектов «{input}».");
        }
        if (errors.Count > 0)
        {
            throw new DrpConfigurationException(errors);
        }
        return new Options(mssql, backupMax, Path.GetFullPath(input!), reportPath, apply);
    }
}
