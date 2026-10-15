using JoinRpg.Common.WebInfrastructure.DataProtection;
using JoinRpg.Common.WebInfrastructure.EfCoreMigration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Мигратор накатывал базы по очереди и падал на кривой строке подключения к одной из них,
/// когда предыдущие уже были мигрированы. Теперь подключение ко всем базам проверяется до первой миграции.
/// </summary>
[Collection(nameof(EnvironmentExitCodeCollection))]
public class MigrationsLauncherTest
{
    [Fact]
    public async Task BadConnectionStringStopsBeforeAnyMigration()
    {
        var first = new RecordingMigrator();

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        // Строка подключения в виде URI — Npgsql падает на разборе хоста, как на проде.
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DataProtection"] = "Host=postgresql://master.example.com;database=x;username=x;password=x",
        });
        builder.Services.AddMigrationsLauncher();
        builder.Services.AddSingleton<IMigratorService>(first);
        builder.Services.RegisterMigrator<DataProtectionDbContext>(builder.Configuration, builder.Environment, "DataProtection");

        try
        {
            using var host = builder.Build();
            await host.RunAsync().WaitAsync(TimeSpan.FromSeconds(30));

            first.Checked.ShouldBeTrue();
            first.Migrated.ShouldBeFalse();
            Environment.ExitCode.ShouldBe(1);
        }
        finally
        {
            Environment.ExitCode = 0;
        }
    }

    [Fact]
    public async Task AllChecksPassedThenAllMigrated()
    {
        var first = new RecordingMigrator();
        var second = new RecordingMigrator();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddMigrationsLauncher();
        builder.Services.AddSingleton<IMigratorService>(first);
        builder.Services.AddSingleton<IMigratorService>(second);

        using var host = builder.Build();
        await host.RunAsync().WaitAsync(TimeSpan.FromSeconds(30));

        first.Migrated.ShouldBeTrue();
        second.Migrated.ShouldBeTrue();
        Environment.ExitCode.ShouldBe(0);
    }

    private class RecordingMigrator : IMigratorService
    {
        public bool Checked { get; private set; }
        public bool Migrated { get; private set; }

        public string Name => nameof(RecordingMigrator);

        public Task CheckConnectionAsync(CancellationToken ct)
        {
            Checked = true;
            return Task.CompletedTask;
        }

        public Task MigrateAsync(CancellationToken ct)
        {
            Migrated = true;
            return Task.CompletedTask;
        }
    }
}

/// <summary>
/// Лаунчер пишет код возврата в глобальный <see cref="Environment.ExitCode"/>, поэтому такие тесты не параллелим.
/// </summary>
[CollectionDefinition(nameof(EnvironmentExitCodeCollection), DisableParallelization = true)]
public class EnvironmentExitCodeCollection;
