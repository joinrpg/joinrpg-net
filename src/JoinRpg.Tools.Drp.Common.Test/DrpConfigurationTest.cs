namespace JoinRpg.Tools.Drp.Common.Test;

/// <summary>
/// Проверяет файл фактов инцидента, который лежит в репозитории и копируется в выход сборки.
/// User-secrets и переменные окружения машины не подключаются.
/// </summary>
public class DrpConfigurationTest
{
    private static DrpCommandLine CommandLine(params string[] args)
        => DrpCommandLine.Parse(args, new Dictionary<string, string>(), []).CommandLine!;

    [Fact]
    public void SettingsFileIsCopiedToOutput()
        => File.Exists(Path.Combine(AppContext.BaseDirectory, DrpConfiguration.SettingsFileName)).ShouldBeTrue();

    [Fact]
    public void SettingsFileHasLostAtOfIncident()
    {
        var settings = new DrpSettings(DrpConfiguration.Build(CommandLine(), includeMachineSources: false));

        settings.LostAt().ShouldBe(new DateTimeOffset(2026, 10, 7, 22, 30, 49, TimeSpan.Zero));
        settings.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void SettingsFileDeclaresBackupMaxForUsers()
        => DrpConfiguration.Build(CommandLine(), includeMachineSources: false)
            .GetSection("Drp:BackupMax").GetChildren().Select(c => c.Key).ShouldContain("Users");

    [Fact]
    public void CommandLineOverridesSettingsFile()
    {
        var configuration = DrpConfiguration.Build(CommandLine("--lost-at", "2026-01-01T00:00:00Z"), includeMachineSources: false);

        new DrpSettings(configuration).LostAt().ShouldBe(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }
}
