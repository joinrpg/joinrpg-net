namespace JoinRpg.Tools.RestoreLostProjects.Test;

public class OptionsAndReportTest
{
    private const string LostAtText = "2026-10-07T22:30:49Z";
    private static readonly DateTimeOffset LostAt = new(2026, 10, 7, 22, 30, 49, TimeSpan.Zero);

    private static string? NoEnvironment(string _) => null;

    private static string[] Args(params string[] extra)
        => ["--mssql", "ms", "--notifications", "pg", "--backup-max-project-id", "3500", "--lost-at", LostAtText, "--report", "r.csv", .. extra];

    [Fact]
    public void ParsesAllOptions()
    {
        var (options, error) = Options.Parse(Args("--logs", "a.json", "--logs", "b.json"), NoEnvironment);

        error.ShouldBeNull();
        options.ShouldNotBeNull();
        options.MssqlConnectionString.ShouldBe("ms");
        options.NotificationsConnectionString.ShouldBe("pg");
        options.BackupMaxProjectId.ShouldBe(3500);
        options.LostAt.ShouldBe(LostAt);
        options.ReportPath.ShouldBe("r.csv");
        options.LogPaths.ShouldBe(["a.json", "b.json"]);
    }

    [Fact]
    public void LogsAreOptional() => Options.Parse(Args(), NoEnvironment).Options!.LogPaths.ShouldBeEmpty();

    [Fact]
    public void ConnectionStringsFromEnvironment()
    {
        var env = new Dictionary<string, string>
        {
            [Options.MssqlEnvironmentVariable] = "ms",
            [Options.NotificationsEnvironmentVariable] = "pg",
        };

        var (options, _) = Options.Parse(["--backup-max-project-id", "1", "--lost-at", LostAtText, "--report", "r.csv"], env.GetValueOrDefault);

        options.ShouldNotBeNull();
        options.MssqlConnectionString.ShouldBe("ms");
        options.NotificationsConnectionString.ShouldBe("pg");
    }

    [Theory]
    [InlineData("--mssql", "ms", "--notifications", "pg", "--lost-at", LostAtText, "--report", "r.csv")]
    [InlineData("--mssql", "ms", "--notifications", "pg", "--backup-max-project-id", "-5", "--lost-at", LostAtText, "--report", "r.csv")]
    [InlineData("--mssql", "ms", "--notifications", "pg", "--backup-max-project-id", "1", "--lost-at", LostAtText)]
    [InlineData("--mssql", "ms", "--notifications", "pg", "--backup-max-project-id", "1", "--lost-at", "2026-10-07T22:30:49", "--report", "r.csv")]
    [InlineData("--mssql", "ms", "--notifications", "pg", "--backup-max-project-id", "1", "--lost-at", LostAtText, "--report", "r.csv", "--apply")]
    [InlineData("--mssql", "ms", "--notifications", "pg", "--backup-max-project-id", "1", "--lost-at", LostAtText, "--report", "r.csv", "--logs")]
    public void RejectsInvalidArguments(params string[] args)
    {
        var (options, error) = Options.Parse(args, NoEnvironment);

        options.ShouldBeNull();
        error.ShouldNotBeNullOrEmpty();
    }

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
