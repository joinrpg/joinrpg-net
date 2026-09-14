using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using JoinRpg.Dal.Impl.Migrations;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.Portal;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

public class JoinApplicationFactory : WebApplicationFactory<Startup>, IAsyncLifetime
{
    /// <summary>
    /// Своя база на общем SQL Server — см. <see cref="SharedSqlServerContainer"/>.
    /// </summary>
    private string connectionString = null!;

    async Task IAsyncLifetime.InitializeAsync()
    {
        Log("Creating test database...");
        connectionString = await SharedSqlServerContainer.CreateDatabaseAsync();
        Log("Test database created.");

        Log("Running EF6 migrations...");
        var migConfig = new Configuration();
        migConfig.TargetDatabase = new DbConnectionInfo(connectionString, "System.Data.SqlClient");
        new DbMigrator(migConfig).Update();
        Log("EF6 migrations done.");

        Log("Building web host...");
        _ = Server;
        Log("Web host built.");
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        // Базу не удаляем: общий контейнер вместе со всеми базами снесёт resource reaper Testcontainers.
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
        builder.UseSetting("ConnectionStrings:DataProtection", "");
        builder.UseSetting("ConnectionStrings:DailyJob", "");
        builder.UseSetting("ConnectionStrings:Notifications", "");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<INotificationRepository>();
            services.AddSingleton<INotificationRepository, NullNotificationRepository>();
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
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [JoinFactory] {message}");
}
