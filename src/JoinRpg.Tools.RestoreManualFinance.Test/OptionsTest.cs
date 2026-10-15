using JoinRpg.Tools.Drp.Common;
using Microsoft.Extensions.Configuration;

namespace JoinRpg.Tools.RestoreManualFinance.Test;

public class OptionsTest : IDisposable
{
    private static readonly DateTimeOffset StartedAt = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
    private readonly string dir = Path.Combine(Path.GetTempPath(), "drp-test-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private IConfiguration Configuration(params (string Key, string? Value)[] overrides)
    {
        var values = new Dictionary<string, string?>
        {
            [DrpKeys.ConnectionString(DrpKeys.MainConnectionName)] = "ms",
            [DrpKeys.ConnectionString(DrpKeys.NotificationsConnectionName)] = "pg",
            [DrpKeys.LostAt] = "2026-10-07T22:30:49Z",
            [DrpKeys.BackupTakenAt] = "2026-05-08T12:58:51Z",
            [DrpKeys.ReportDir] = dir,
        };
        foreach (var (key, value) in overrides)
        {
            values[key] = value;
        }
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void DryRunReadsAllValues()
    {
        var options = Options.FromConfiguration(Configuration(), apply: false, StartedAt);

        options.MssqlConnectionString.ShouldBe("ms");
        options.NotificationsConnectionString.ShouldBe("pg");
        options.LostAt.ShouldBe(new DateTimeOffset(2026, 10, 7, 22, 30, 49, TimeSpan.Zero));
        options.BackupTakenAt.ShouldBe(new DateTimeOffset(2026, 5, 8, 12, 58, 51, TimeSpan.Zero));
        options.ReportPath.ShouldBe(Path.Combine(Path.GetFullPath(dir), "restore-manual-finance-20261015-120000Z.csv"));
        options.Apply.ShouldBeFalse();
        options.InputPath.ShouldBeNull();
    }

    [Fact]
    public void ApplyNeedsExistingInput()
    {
        Should.Throw<DrpConfigurationException>(() => Options.FromConfiguration(Configuration(), apply: true, StartedAt))
            .Errors.ShouldHaveSingleItem().ShouldContain(Options.InputOption);

        Should.Throw<DrpConfigurationException>(() => Options.FromConfiguration(
                Configuration((Options.InputKey, Path.Combine(dir, "нет.csv"))), apply: true, StartedAt))
            .Errors.ShouldHaveSingleItem().ShouldContain("Не найден");
    }

    [Fact]
    public void ApplyTakesInputPath()
    {
        _ = Directory.CreateDirectory(dir);
        var input = Path.Combine(dir, "plan.csv");
        File.WriteAllText(input, "");

        var options = Options.FromConfiguration(Configuration((Options.InputKey, input)), apply: true, StartedAt);

        options.Apply.ShouldBeTrue();
        options.InputPath.ShouldBe(Path.GetFullPath(input));
    }

    [Fact]
    public void InputWithoutApplyIsError()
        => Should.Throw<DrpConfigurationException>(() => Options.FromConfiguration(Configuration((Options.InputKey, "plan.csv")), apply: false, StartedAt))
            .Errors.ShouldHaveSingleItem().ShouldContain(Options.ApplyFlag);

    [Fact]
    public void BackupMustBeBeforeLoss()
        => Should.Throw<DrpConfigurationException>(() => Options.FromConfiguration(
                Configuration((DrpKeys.BackupTakenAt, "2026-10-08T00:00:00Z")), apply: false, StartedAt))
            .Errors.ShouldHaveSingleItem().ShouldContain(DrpKeys.BackupTakenAt);

    [Fact]
    public void CollectsAllErrorsAtOnce()
    {
        var errors = Should.Throw<DrpConfigurationException>(() => Options.FromConfiguration(
                Configuration(
                    (DrpKeys.ConnectionString(DrpKeys.NotificationsConnectionName), null),
                    (DrpKeys.BackupTakenAt, null)),
                apply: true,
                StartedAt))
            .Errors;

        errors.Count.ShouldBe(3);
        errors[0].ShouldContain("ConnectionStrings:Notifications");
        errors[1].ShouldContain(DrpKeys.BackupTakenAt);
        errors[2].ShouldContain(Options.InputOption);
    }

    [Fact]
    public void CommandLineKnowsToolOptions()
    {
        var (commandLine, error) = DrpCommandLine.Parse(
            ["--apply", "--input", "plan.csv", "--backup-taken-at", "2026-05-01T00:00:00Z"], Options.CommandLineOptions, Options.CommandLineFlags);

        error.ShouldBeNull();
        commandLine!.HasFlag(Options.ApplyFlag).ShouldBeTrue();
        commandLine.Overrides[Options.InputKey].ShouldBe("plan.csv");
        commandLine.Overrides[DrpKeys.BackupTakenAt].ShouldBe("2026-05-01T00:00:00Z");
    }

    [Fact]
    public void UsageMentionsEditableColumn() => Options.Usage.ShouldContain($"«{ManualFinanceCsv.PaymentType}»");
}
