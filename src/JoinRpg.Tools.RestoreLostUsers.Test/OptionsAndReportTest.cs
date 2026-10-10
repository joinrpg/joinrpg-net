using JoinRpg.Tools.Drp.Common;
using Microsoft.Extensions.Configuration;

namespace JoinRpg.Tools.RestoreLostUsers.Test;

public class OptionsAndReportTest
{
    private static readonly DateTimeOffset LostAt = new(2026, 10, 7, 22, 30, 49, TimeSpan.Zero);
    private static readonly DateTimeOffset StartedAt = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

    // Свой каталог на экземпляр теста: ReportPath создаёт каталог, тест его удаляет.
    private readonly string reportFile = Path.Combine(Path.GetTempPath(), "drp-restore-test-" + Guid.NewGuid().ToString("N"), "r.csv");

    private Dictionary<string, string?> Complete() => new()
    {
        ["ConnectionStrings:DefaultConnection"] = "ms",
        ["ConnectionStrings:Notifications"] = "pg",
        [DrpKeys.LostAt] = "2026-10-07T22:30:49Z",
        [DrpKeys.BackupMax("Users")] = "75000",
        [DrpKeys.ReportPath] = reportFile,
    };

    private static Options FromConfiguration(Dictionary<string, string?> values, bool apply = false)
        => Options.FromConfiguration(new ConfigurationBuilder().AddInMemoryCollection(values).Build(), apply, StartedAt);

    [Fact]
    public void ReadsAllOptionsFromConfiguration()
    {
        try
        {
            FromConfiguration(Complete(), apply: true).ShouldBe(new Options("ms", "pg", 75000, LostAt, Path.GetFullPath(reportFile), Apply: true));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(reportFile)!, recursive: true);
        }
    }

    [Fact]
    public void ReportsEveryMissingKey()
    {
        var values = Complete();
        _ = values.Remove("ConnectionStrings:Notifications");
        values[DrpKeys.BackupMax("Users")] = "";

        var exception = Should.Throw<DrpConfigurationException>(() => FromConfiguration(values));
        Directory.Delete(Path.GetDirectoryName(reportFile)!, recursive: true);

        exception.Errors.Count.ShouldBe(2);
        exception.Errors.ShouldContain(e => e.Contains("ConnectionStrings:Notifications"));
        exception.Errors.ShouldContain(e => e.Contains(Options.BackupMaxOption));
    }

    [Fact]
    public void CommandLineKnowsToolOptionsAndApplyFlag()
    {
        var (commandLine, error) = DrpCommandLine.Parse(
            [Options.ApplyFlag, Options.BackupMaxOption, "75001"], Options.CommandLineOptions, Options.CommandLineFlags);

        error.ShouldBeNull();
        commandLine!.HasFlag(Options.ApplyFlag).ShouldBeTrue();
        commandLine.Overrides[DrpKeys.BackupMax("Users")].ShouldBe("75001");
    }

    [Theory]
    [InlineData("--mssql", "ms")]
    [InlineData("--notifications", "pg")]
    public void ConnectionStringsAreNotCommandLineOptions(params string[] args)
        => DrpCommandLine.Parse(args, Options.CommandLineOptions, Options.CommandLineFlags).CommandLine.ShouldBeNull();

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("with;semicolon", "\"with;semicolon\"")]
    [InlineData("with \"quote\"", "\"with \"\"quote\"\"\"")]
    [InlineData("two\nlines", "\"two\nlines\"")]
    [InlineData(null, "")]
    public void EscapesCsvValues(string? value, string expected) => ReportWriter.Escape(value).ShouldBe(expected);

    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("@user", "'@user")]
    [InlineData("-cmd@example.com", "'-cmd@example.com")]
    [InlineData("\t=1+1", "'\t=1+1")]
    [InlineData("\r=1+1", "'\r=1+1")]
    [InlineData("Иван", "Иван")]
    public void GuardsAgainstSpreadsheetFormulas(string value, string expected) => ReportWriter.UserText(value).ShouldBe(expected);
}
