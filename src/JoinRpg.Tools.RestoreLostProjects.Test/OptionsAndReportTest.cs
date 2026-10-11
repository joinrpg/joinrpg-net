using JoinRpg.Tools.Drp.Common;
using Microsoft.Extensions.Configuration;

namespace JoinRpg.Tools.RestoreLostProjects.Test;

public sealed class OptionsAndReportTest : IDisposable
{
    private static readonly DateTimeOffset LostAt = new(2026, 10, 7, 22, 30, 49, TimeSpan.Zero);
    private static readonly DateTimeOffset StartedAt = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

    // Свой каталог на экземпляр теста: ReportPath создаёт каталог, тест его удаляет.
    private readonly string dir = Path.Combine(Path.GetTempPath(), "drp-projects-test-" + Guid.NewGuid().ToString("N"));

    private string ReportFile => Path.Combine(dir, "r.csv");

    public void Dispose()
    {
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private string LogsFile => Path.Combine(dir, "k8s-prod-mutations-merged.json");

    private Dictionary<string, string?> Complete()
    {
        _ = Directory.CreateDirectory(dir);
        File.WriteAllText(LogsFile, "[]");
        return new()
        {
            ["ConnectionStrings:DefaultConnection"] = "ms",
            ["ConnectionStrings:Notifications"] = "pg",
            [DrpKeys.LostAt] = "2026-10-07T22:30:49Z",
            [DrpKeys.BackupMax("Projects")] = "3500",
            [DrpKeys.ReportPath] = ReportFile,
            [Options.LogsKey] = LogsFile,
        };
    }

    private static Options FromConfiguration(Dictionary<string, string?> values)
        => Options.FromConfiguration(new ConfigurationBuilder().AddInMemoryCollection(values).Build(), StartedAt);

    [Fact]
    public void ReadsAllOptionsFromConfiguration()
    {
        var options = FromConfiguration(Complete());

        options.MssqlConnectionString.ShouldBe("ms");
        options.NotificationsConnectionString.ShouldBe("pg");
        options.BackupMaxProjectId.ShouldBe(3500);
        options.LostAt.ShouldBe(LostAt);
        options.ReportPath.ShouldBe(Path.GetFullPath(ReportFile));
        options.LogPaths.ShouldBe([Path.GetFullPath(LogsFile)]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void LogsAreRequired(string? value)
    {
        var values = Complete();
        values[Options.LogsKey] = value;

        var exception = Should.Throw<DrpConfigurationException>(() => FromConfiguration(values));

        exception.Errors.ShouldHaveSingleItem().ShouldContain(Options.LogsKey);
    }

    [Fact]
    public void LogsFromDirectoryTakeJsonFilesRecursively()
    {
        var values = Complete();
        var logs = Path.Combine(dir, "logs");
        _ = Directory.CreateDirectory(Path.Combine(logs, "portal"));
        File.WriteAllText(Path.Combine(logs, "b.ndjson"), "");
        File.WriteAllText(Path.Combine(logs, "portal", "a.json"), "");
        File.WriteAllText(Path.Combine(logs, "readme.txt"), "");
        values[Options.LogsKey] = logs;

        FromConfiguration(values).LogPaths.Select(Path.GetFileName).ShouldBe(["b.ndjson", "a.json"], ignoreOrder: true);
    }

    [Fact]
    public void ReportsEveryMissingKeyIncludingLogs()
    {
        var values = Complete();
        _ = values.Remove("ConnectionStrings:Notifications");
        values[DrpKeys.BackupMax("Projects")] = "";
        values[Options.LogsKey] = Path.Combine(dir, "нет-такого");

        var exception = Should.Throw<DrpConfigurationException>(() => FromConfiguration(values));

        exception.Errors.Count.ShouldBe(3);
        exception.Errors.ShouldContain(e => e.Contains("ConnectionStrings:Notifications"));
        exception.Errors.ShouldContain(e => e.Contains(Options.BackupMaxOption));
        exception.Errors.ShouldContain(e => e.Contains(Options.LogsKey));
    }

    [Fact]
    public void CommandLineKnowsToolOptions()
    {
        var (commandLine, error) = DrpCommandLine.Parse(
            [Options.BackupMaxOption, "3501", Options.LogsOption, "logs"], Options.CommandLineOptions, Options.CommandLineFlags);

        error.ShouldBeNull();
        commandLine!.Overrides[DrpKeys.BackupMax("Projects")].ShouldBe("3501");
        commandLine.Overrides[Options.LogsKey].ShouldBe("logs");
    }

    [Fact]
    public void CommandLineHasNoApplyFlag()
        => DrpCommandLine.Parse(["--apply"], Options.CommandLineOptions, Options.CommandLineFlags).Error.ShouldNotBeNull();

    [Fact]
    public void ReportEscapesAndProtectsFromFormulas()
    {
        var report = new LostProjectReport(
            1001,
            "=Дюна; \"Пробуждение\"",
            [new NameVariant("=Дюна; \"Пробуждение\"", new Dictionary<HeaderKind, int> { [HeaderKind.Claim] = 3, [HeaderKind.AdminNewProject] = 1 }, new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.Zero))],
            2001,
            "master@example.com",
            CreatorMethod.Presumed,
            [new CreatorCandidate(2002, 4, new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero))],
            new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero),
            4,
            0,
            [],
            null,
            new KogdaIgraLink(true, 77),
            null,
            ProjectFlags.NameConflict | ProjectFlags.CreatorMissingInMssql);

        var lines = ReportWriter.Build([report]).Split("\r\n");

        lines[0].ShouldStartWith("ProjectId;Название;");
        lines[1].ShouldBe(
            "1001;\"'=Дюна; \"\"Пробуждение\"\"\";\"'=Дюна; \"\"Пробуждение\"\" (админам о новом проекте: 1, заявки: 3, последнее 2026-08-30)\";"
            + "2001;master@example.com;предположительно;2002 (4 свид., с 2026-07-01);"
            + "2026-06-01 12:00:00;2026-09-01 12:00:00;4;0;;;/game/77/;;конфликт названий, создатель отсутствует в MSSQL");
    }
}
