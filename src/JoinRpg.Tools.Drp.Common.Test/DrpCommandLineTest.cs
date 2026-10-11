namespace JoinRpg.Tools.Drp.Common.Test;

public class DrpCommandLineTest
{
    private static readonly Dictionary<string, string> ToolOptions = new() { ["--backup-max-user-id"] = DrpKeys.BackupMax("Users") };
    private static readonly string[] Flags = ["--apply"];

    private static (DrpCommandLine? CommandLine, string? Error) Parse(params string[] args) => DrpCommandLine.Parse(args, ToolOptions, Flags);

    [Fact]
    public void EmptyIsValid()
    {
        var (commandLine, error) = Parse();

        error.ShouldBeNull();
        commandLine!.Overrides.ShouldBeEmpty();
        commandLine.HasFlag("--apply").ShouldBeFalse();
    }

    [Fact]
    public void ParsesFlagsCommonAndToolOptions()
    {
        var (commandLine, error) = Parse("--apply", "--lost-at", "2026-10-07T22:30:49Z", "--report", "r.csv", "--backup-max-user-id", "75000");

        error.ShouldBeNull();
        commandLine!.HasFlag("--apply").ShouldBeTrue();
        commandLine.Overrides[DrpKeys.LostAt].ShouldBe("2026-10-07T22:30:49Z");
        commandLine.Overrides[DrpKeys.ReportPath].ShouldBe("r.csv");
        commandLine.Overrides[DrpKeys.BackupMax("Users")].ShouldBe("75000");
    }

    [Theory]
    [InlineData("--force")]
    [InlineData("--lost-at")]
    [InlineData("--apply", "--mssql", "x")]
    [InlineData("apply")]
    public void RejectsUnknownOrIncomplete(params string[] args)
    {
        var (commandLine, error) = Parse(args);

        commandLine.ShouldBeNull();
        error.ShouldNotBeNullOrWhiteSpace();
    }
}
