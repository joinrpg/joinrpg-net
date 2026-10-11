using Microsoft.Extensions.Configuration;

namespace JoinRpg.Tools.Drp.Common;

/// <summary>
/// Конфигурация DRP-инструментов. Слои по возрастанию приоритета:
/// 1. drp.appsettings.json из репозитория — факты инцидента (момент аварии, максимумы id бэкапа);
/// 2. user-secrets с общим id joinrpg-drp — строки подключения;
/// 3. переменные окружения с префиксом DRP_ (DRP_ConnectionStrings__DefaultConnection);
/// 4. разовые переопределения из командной строки.
/// </summary>
public static class DrpConfiguration
{
    /// <summary>Один и тот же во всех DRP-инструментах: строки подключения задаются один раз.</summary>
    public const string UserSecretsId = "joinrpg-drp";

    public const string EnvironmentPrefix = "DRP_";

    public const string SettingsFileName = "drp.appsettings.json";

    public static IConfigurationRoot Build(DrpCommandLine commandLine) => Build(commandLine, includeMachineSources: true);

    /// <param name="commandLine">Разобранная командная строка.</param>
    /// <param name="includeMachineSources">false — без user-secrets и переменных окружения, для тестов.</param>
    internal static IConfigurationRoot Build(DrpCommandLine commandLine, bool includeMachineSources)
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, SettingsFileName), optional: false, reloadOnChange: false);
        if (includeMachineSources)
        {
            _ = builder
                .AddUserSecrets(UserSecretsId, reloadOnChange: false)
                .AddEnvironmentVariables(EnvironmentPrefix);
        }
        return builder
            .AddInMemoryCollection(commandLine.Overrides)
            .Build();
    }

    /// <summary>
    /// Справка о том, откуда берутся настройки, — для Usage каждого инструмента.
    /// </summary>
    public static string SourcesHelp => $"""
        Настройки (по возрастанию приоритета):
          {SettingsFileName,-30} факты инцидента: Drp:LostAt, Drp:BackupMax:<таблица>, Drp:ReportDir (в репозитории)
          {"user-secrets " + UserSecretsId,-30} строки подключения ConnectionStrings:{DrpKeys.MainConnectionName} (MSSQL) и ConnectionStrings:{DrpKeys.NotificationsConnectionName} (Postgres):
          {"",-30} dotnet user-secrets set --id {UserSecretsId} "ConnectionStrings:{DrpKeys.MainConnectionName}" "<строка>"
          {"переменные " + EnvironmentPrefix + "*",-30} например {EnvironmentPrefix}ConnectionStrings__{DrpKeys.MainConnectionName}
          {"командная строка",-30} разовые переопределения
        Отчёты с персональными данными пишутся в Drp:ReportDir (по умолчанию {DrpSettings.DefaultReportDir}).
        """;
}
