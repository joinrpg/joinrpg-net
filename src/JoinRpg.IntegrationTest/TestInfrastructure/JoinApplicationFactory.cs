using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using JoinRpg.Dal.Impl.Migrations;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.Portal;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

public class JoinApplicationFactory : WebApplicationFactory<Startup>, IAsyncLifetime
{
    /// <summary>
    /// Строка подключения к тестовой базе этой фабрики — для тестов, которым нужен прямой SQL
    /// (например проверка, что база выровнена по проду).
    /// </summary>
    internal string ConnectionString { get; private set; } = null!;

    /// <summary>Замеры ленивых загрузок по запросам этой фабрики (#4914).</summary>
    internal LazyLoadObservations Observations { get; } = new();

    /// <summary>
    /// Клиент со всей штатной обвязкой плюс проверка ленивых загрузок.
    /// </summary>
    /// <remarks>
    /// Скрывает <see cref="WebApplicationFactory{TEntryPoint}.CreateClient()"/>, чтобы проверка
    /// подключалась ко всем тестам разом. Цепочку обработчиков приходится собирать руками:
    /// <c>WebApplicationFactoryClientOptions.CreateHandlers()</c> — internal.
    /// </remarks>
    public new HttpClient CreateClient() => CreateClient(ClientOptions);

    /// <inheritdoc cref="CreateClient()"/>
    public new HttpClient CreateClient(WebApplicationFactoryClientOptions options)
    {
        var handlers = new List<DelegatingHandler>();
        if (options.AllowAutoRedirect)
        {
            handlers.Add(new RedirectHandler(options.MaxAutomaticRedirections));
        }
        if (options.HandleCookies)
        {
            handlers.Add(new CookieContainerHandler());
        }
        handlers.Add(new LazyLoadAssertingHandler(Observations, LazyLoadBaseline.Instance));

        return CreateDefaultClient(options.BaseAddress, [.. handlers]);
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        Log("Creating test database...");
        ConnectionString = await SharedSqlServerContainer.CreateDatabaseAsync();
        Log("Test database created.");

        Log("Running EF6 migrations...");
        var migConfig = new Configuration();
        migConfig.TargetDatabase = new DbConnectionInfo(ConnectionString, "System.Data.SqlClient");
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

        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("ConnectionStrings:DataProtection", "");
        builder.UseSetting("ConnectionStrings:DailyJob", "");
        builder.UseSetting("ConnectionStrings:Notifications", "");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<INotificationRepository>();
            services.AddSingleton<INotificationRepository, NullNotificationRepository>();
            services.AddDataProtection().UseEphemeralDataProtectionProvider();

            services.AddSingleton(Observations);
            // Middleware в пайплайн Startup иначе не вставить.
            services.AddTransient<IStartupFilter, LazyLoadObservingStartupFilter>();
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
