using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace JoinRpg.Tools.Drp.Common;

/// <summary>
/// Чтение и проверка настроек DRP-инструмента. Ошибки накапливаются, чтобы пользователь увидел
/// сразу все недостающие ключи, а не по одному за запуск; <see cref="ThrowIfErrors"/> бросает их разом.
/// </summary>
public sealed class DrpSettings(IConfiguration configuration)
{
    private readonly List<string> errors = [];

    public IReadOnlyList<string> Errors => errors;

    public static string DefaultReportDir
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "joinrpg-drp", "reports");

    // Только с явным часовым поясом: момент без пояса молча истолковался бы по часам машины.
    private static readonly string[] MomentFormats =
    [
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz",
    ];

    /// <summary>Разбирает момент в формате RFC 3339 с обязательным часовым поясом.</summary>
    public static bool TryParseMoment(string? value, out DateTimeOffset moment)
        => DateTimeOffset.TryParseExact(value, MomentFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out moment);

    public string ConnectionString(string name, string description)
    {
        var value = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(
                $"Не задана строка подключения {DrpKeys.ConnectionString(name)} ({description}). "
                + $"Задайте её: dotnet user-secrets set --id {DrpConfiguration.UserSecretsId} \"{DrpKeys.ConnectionString(name)}\" \"<строка>\" "
                + $"или переменной окружения {DrpConfiguration.EnvironmentPrefix}ConnectionStrings__{name}.");
            return "";
        }
        return value;
    }

    public DateTimeOffset LostAt()
    {
        var value = configuration[DrpKeys.LostAt];
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"Не задан момент аварии {DrpKeys.LostAt} (в {DrpConfiguration.SettingsFileName} или --lost-at).");
            return default;
        }
        if (!TryParseMoment(value, out var moment))
        {
            errors.Add($"Некорректен момент аварии {DrpKeys.LostAt} = «{value}»: нужен RFC 3339 с часовым поясом, например 2026-10-07T22:30:49Z.");
            return default;
        }
        return moment;
    }

    /// <param name="table">Таблица, например Users.</param>
    /// <param name="commandLineOption">Параметр командной строки, которым можно переопределить значение.</param>
    public int BackupMax(string table, string commandLineOption)
    {
        var key = DrpKeys.BackupMax(table);
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(
                $"Не задан максимальный id таблицы {table} в восстановленном бэкапе ({key}). "
                + $"Выполните сразу после восстановления SELECT MAX(<первичный ключ>) FROM dbo.{table} и впишите результат в {DrpConfiguration.SettingsFileName} "
                + $"(или передайте {commandLineOption}).");
            return 0;
        }
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result) || result <= 0)
        {
            errors.Add($"Некорректен {key} = «{value}»: нужно положительное целое число.");
            return 0;
        }
        return result;
    }

    /// <summary>
    /// Путь к отчёту: --report, если задан, иначе файл с временем запуска в Drp:ReportDir.
    /// Каталог создаётся.
    /// </summary>
    /// <param name="toolName">Короткое имя инструмента — начало имени файла.</param>
    /// <param name="startedAt">Время запуска — в имени файла, чтобы отчёты не затирали друг друга.</param>
    public string ReportPath(string toolName, DateTimeOffset startedAt)
    {
        var explicitPath = configuration[DrpKeys.ReportPath];
        string path;
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            path = Path.GetFullPath(explicitPath);
        }
        else
        {
            var configuredDir = configuration[DrpKeys.ReportDir];
            var dir = string.IsNullOrWhiteSpace(configuredDir) ? DefaultReportDir : configuredDir;
            var fileName = $"{toolName}-{startedAt.UtcDateTime.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}Z.csv";
            path = Path.GetFullPath(Path.Combine(dir, fileName));
        }

        try
        {
            _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            errors.Add($"Не удалось создать каталог для отчёта {Path.GetDirectoryName(path)}: {e.Message}");
        }
        return path;
    }

    public void ThrowIfErrors()
    {
        if (errors.Count > 0)
        {
            throw new DrpConfigurationException(errors);
        }
    }
}

public sealed class DrpConfigurationException(IReadOnlyList<string> errors)
    : Exception(string.Join(Environment.NewLine, errors))
{
    public IReadOnlyList<string> Errors { get; } = [.. errors];
}
