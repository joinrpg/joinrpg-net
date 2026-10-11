using Microsoft.Extensions.Configuration;

namespace JoinRpg.Tools.Drp.Common.Test;

public class DrpSettingsTest
{
    private static readonly DateTimeOffset LostAt = new(2026, 10, 7, 22, 30, 49, TimeSpan.Zero);

    private static DrpSettings Settings(params (string Key, string? Value)[] values)
        => new(new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build());

    [Theory]
    [InlineData("2026-10-07T22:30:49Z")]
    [InlineData("2026-10-07T22:30:49.000Z")]
    [InlineData("2026-10-08T01:30:49+03:00")]
    public void ParsesMomentWithTimeZone(string value)
    {
        DrpSettings.TryParseMoment(value, out var moment).ShouldBeTrue();
        moment.ShouldBe(LostAt);
    }

    [Theory]
    [InlineData("2026-10-07T22:30:49")]
    [InlineData("2026-10-07")]
    [InlineData("вчера")]
    [InlineData("")]
    [InlineData(null)]
    public void RejectsMomentWithoutTimeZone(string? value) => DrpSettings.TryParseMoment(value, out _).ShouldBeFalse();

    [Fact]
    public void ReadsAllValues()
    {
        var settings = Settings(
            ("ConnectionStrings:DefaultConnection", "ms"),
            (DrpKeys.LostAt, "2026-10-07T22:30:49Z"),
            (DrpKeys.BackupMax("Users"), "75000"));

        settings.ConnectionString(DrpKeys.MainConnectionName, "MSSQL").ShouldBe("ms");
        settings.LostAt().ShouldBe(LostAt);
        settings.BackupMax("Users", "--backup-max-user-id").ShouldBe(75000);
        settings.Errors.ShouldBeEmpty();
        Should.NotThrow(settings.ThrowIfErrors);
    }

    [Fact]
    public void CollectsAllErrorsAtOnce()
    {
        // BackupMax: null в drp.appsettings.json приходит в конфигурацию пустой строкой.
        var settings = Settings((DrpKeys.LostAt, "2026-10-07T22:30:49"), (DrpKeys.BackupMax("Users"), ""));

        _ = settings.ConnectionString(DrpKeys.MainConnectionName, "MSSQL");
        _ = settings.ConnectionString(DrpKeys.NotificationsConnectionName, "Postgres");
        _ = settings.LostAt();
        _ = settings.BackupMax("Users", "--backup-max-user-id");

        settings.Errors.Count.ShouldBe(4);
        settings.Errors[0].ShouldContain("ConnectionStrings:DefaultConnection");
        settings.Errors[0].ShouldContain("dotnet user-secrets set --id joinrpg-drp");
        settings.Errors[0].ShouldContain("DRP_ConnectionStrings__DefaultConnection");
        settings.Errors[1].ShouldContain("ConnectionStrings:Notifications");
        settings.Errors[2].ShouldContain("часовым поясом");
        settings.Errors[3].ShouldContain("Drp:BackupMax:Users");
        settings.Errors[3].ShouldContain("--backup-max-user-id");

        Should.Throw<DrpConfigurationException>(settings.ThrowIfErrors).Errors.Count.ShouldBe(4);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("много")]
    public void RejectsInvalidBackupMax(string value)
    {
        var settings = Settings((DrpKeys.BackupMax("Users"), value));

        _ = settings.BackupMax("Users", "--backup-max-user-id");

        settings.Errors.ShouldHaveSingleItem().ShouldContain("положительное целое");
    }

    [Fact]
    public void MissingLostAtIsError()
    {
        var settings = Settings();

        _ = settings.LostAt();

        settings.Errors.ShouldHaveSingleItem().ShouldContain(DrpKeys.LostAt);
    }

    [Fact]
    public void ReportGoesToReportDirWithStartTimeInName()
    {
        var dir = Path.Combine(Path.GetTempPath(), "drp-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var settings = Settings((DrpKeys.ReportDir, dir));

            var path = settings.ReportPath("restore-lost-users", new DateTimeOffset(2026, 10, 11, 1, 2, 3, TimeSpan.FromHours(3)));

            path.ShouldBe(Path.Combine(Path.GetFullPath(dir), "restore-lost-users-20261010-220203Z.csv"));
            Directory.Exists(dir).ShouldBeTrue();
            settings.Errors.ShouldBeEmpty();
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Fact]
    public void ExplicitReportPathWins()
    {
        var dir = Path.Combine(Path.GetTempPath(), "drp-test-" + Guid.NewGuid().ToString("N"));
        var file = Path.Combine(dir, "sub", "my.csv");
        try
        {
            var settings = Settings((DrpKeys.ReportDir, Path.Combine(dir, "ignored")), (DrpKeys.ReportPath, file));

            settings.ReportPath("restore-lost-users", LostAt).ShouldBe(Path.GetFullPath(file));
            Directory.Exists(Path.Combine(dir, "sub")).ShouldBeTrue();
            Directory.Exists(Path.Combine(dir, "ignored")).ShouldBeFalse();
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Fact]
    public void DefaultReportDirIsUnderLocalAppData()
        => DrpSettings.DefaultReportDir.ShouldBe(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "joinrpg-drp", "reports"));
}
