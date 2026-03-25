using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using JoinRpg.Dal.Impl.Migrations;
using JoinRpg.Portal;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Testcontainers.MsSql;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

public class JoinApplicationFactory : WebApplicationFactory<Startup>, IAsyncLifetime
{
#pragma warning disable CS0618 // use same pattern as IdPortalApplicationFactory
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder().Build();
#pragma warning restore CS0618

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _msSqlContainer.StartAsync();

        var migConfig = new Configuration();
        migConfig.TargetDatabase = new DbConnectionInfo(
            _msSqlContainer.GetConnectionString(), "System.Data.SqlClient");
        new DbMigrator(migConfig).Update();

        _ = Server;
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
}
