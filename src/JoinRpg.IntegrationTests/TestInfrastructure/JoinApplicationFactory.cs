using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using JoinRpg.Dal.Impl.Migrations;
using JoinRpg.Portal;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

public class JoinApplicationFactory : WebApplicationFactory<Startup>, IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder().Build();

    async Task IAsyncLifetime.InitializeAsync()
    {
        Log("Starting SQL Server container...");
        await _msSqlContainer.StartAsync();
        Log("SQL Server container started.");

        Log("Running EF6 migrations...");
        var migConfig = new Configuration();
        migConfig.TargetDatabase = new DbConnectionInfo(
            _msSqlContainer.GetConnectionString(), "System.Data.SqlClient");
        new DbMigrator(migConfig).Update();
        Log("EF6 migrations done.");

        Log("Building web host...");
        _ = Server;
        Log("Web host built.");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _msSqlContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.UseSetting("ConnectionStrings:DefaultConnection", _msSqlContainer.GetConnectionString());

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
        builder.ConfigureLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddConsole();
        });
        _ = builder.UseTestServer();
    }

    private static void Log(string message) =>
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [JoinApplicationFactory] {message}");
}
