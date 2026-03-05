using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using Joinrpg.Web.Identity;
using JoinRpg.Dal.Impl.Migrations;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.IdPortal.OAuthServer;
using JoinRpg.Interfaces;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Notifications;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace JoinRpg.IdPortal.Test.Infrastructure;

public class IdPortalApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder().Build();

    public CaptureEmailService CaptureEmail { get; } = new();

    public const string TestClientId = "integration-test-client";
    public const string TestClientSecret = "integration-test-secret";
    public const string TestRedirectUri = "http://localhost/callback";
    public const string TestUserEmail = "test@joinrpg.ru";
    public const string TestUserPassword = "TestPassword123!";
    public const string TestDisplayName = "Тестовый Пользователь";
    public const string TestResetUserEmail = "reset@joinrpg.ru";
    public const string TestResetUserPassword = "ResetPassword123!";
    public const string TestUnconfirmedUserEmail = "unconfirmed@joinrpg.ru";

    async Task IAsyncLifetime.InitializeAsync()
    {
        Log("Starting containers...");
        await Task.WhenAll(_postgresContainer.StartAsync(), _msSqlContainer.StartAsync());
        Log("Containers started.");

        Log("Running EF6 migrations on SQL Server...");
        var migConfig = new Configuration();
        migConfig.TargetDatabase = new DbConnectionInfo(
            _msSqlContainer.GetConnectionString(), "System.Data.SqlClient");
        new DbMigrator(migConfig).Update();
        Log("EF6 migrations done.");

        Log("Building web host...");
        _ = Server;
        Log("Web host built.");

        using var scope = Services.CreateScope();

        Log("Running EF Core migrations on PostgreSQL...");
        var db = scope.ServiceProvider.GetRequiredService<IdPortalDbContext>();
        await db.Database.MigrateAsync();
        Log("EF Core migrations done.");

        Log("Creating test users...");
        var userManager = scope.ServiceProvider.GetRequiredService<JoinUserManager>();

        var user = new JoinIdentityUser { UserName = TestUserEmail };
        user.DisplayName = new UserDisplayName(TestDisplayName, null);
        await userManager.CreateAsync(user, TestUserPassword);
        user = await userManager.FindByNameAsync(TestUserEmail)
            ?? throw new InvalidOperationException("Test user not found after creation");
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await userManager.ConfirmEmailAsync(user, token);

        var resetUser = new JoinIdentityUser { UserName = TestResetUserEmail };
        await userManager.CreateAsync(resetUser, TestResetUserPassword);
        resetUser = await userManager.FindByNameAsync(TestResetUserEmail)
            ?? throw new InvalidOperationException("Reset test user not found after creation");
        token = await userManager.GenerateEmailConfirmationTokenAsync(resetUser);
        await userManager.ConfirmEmailAsync(resetUser, token);

        var unconfirmedUser = new JoinIdentityUser { UserName = TestUnconfirmedUserEmail };
        await userManager.CreateAsync(unconfirmedUser, TestUserPassword);

        Log("Test users created.");

        Log("Registering OAuth client...");
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = TestClientId,
            ClientSecret = TestClientSecret,
            ClientType = ClientTypes.Confidential,
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.ResponseTypes.Code,
                Permissions.Prefixes.Scope + Scopes.OpenId,
                Permissions.Prefixes.Scope + Scopes.Email,
                Permissions.Prefixes.Scope + Scopes.Profile,
            },
        };
        descriptor.RedirectUris.Add(new Uri(TestRedirectUri));
        await manager.CreateAsync(descriptor);
        Log("OAuth client registered. InitializeAsync complete.");
    }

    private static void Log(string message) =>
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [IdPortalFactory] {message}");

    async Task IAsyncLifetime.DisposeAsync()
    {
        await Task.WhenAll(
            _postgresContainer.DisposeAsync().AsTask(),
            _msSqlContainer.DisposeAsync().AsTask());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.UseSetting("ConnectionStrings:IdPortal", _postgresContainer.GetConnectionString());
        builder.UseSetting("ConnectionStrings:DataProtection", "");
        builder.UseSetting("ConnectionStrings:Notifications", "");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _msSqlContainer.GetConnectionString());
        builder.UseSetting("JoinRpgHostNames:IdHost", "localhost");
        builder.UseSetting("JoinRpgHostNames:MainHost", "localhost");
        builder.UseSetting("JoinRpgHostNames:KogdaIgraHost", "localhost");
        builder.UseSetting("JoinRpgHostNames:RatingHost", "localhost");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();

            services.RemoveAll<IAccountEmailService<JoinIdentityUser>>();
            services.AddSingleton<IAccountEmailService<JoinIdentityUser>>(CaptureEmail);

            services.RemoveAll<INotificationRepository>();
            services.AddTransient<INotificationRepository, NullNotificationRepository>();

            services.RemoveAll<INotificationService>();
            services.AddTransient<INotificationService, NullNotificationService>();

            services.AddDataProtection().UseEphemeralDataProtectionProvider();

            services.Configure<OpenIddictServerAspNetCoreOptions>(options =>
                options.DisableTransportSecurityRequirement = true);
        });

        _ = builder.UseTestServer();
    }

    private sealed class NullNotificationRepository : INotificationRepository
    {
        public Task InsertNotifications(NotificationMessageCreateDto[] notifications) => Task.CompletedTask;

        public Task<TargetedNotificationMessageForRecipient?> SelectNextNotificationForSending(NotificationChannel channel)
            => Task.FromResult<TargetedNotificationMessageForRecipient?>(null);

        public Task MarkSendingSucceeded(NotificationId id, NotificationChannel channel) => Task.CompletedTask;

        public Task MarkSendingFailed(NotificationId id, NotificationChannel channel) => Task.CompletedTask;

        public Task MarkEnqueued(NotificationId id, NotificationChannel channel, DateTimeOffset sendAfter, int? attempts = null)
            => Task.CompletedTask;

        public Task<IReadOnlyCollection<NotificationHistoryDto>> GetLastNotificationsForUser(
            UserIdentification userId, NotificationChannel notificationChannel, KeySetPagination pagination)
            => Task.FromResult<IReadOnlyCollection<NotificationHistoryDto>>([]);
    }

    private sealed class NullNotificationService : INotificationService
    {
        public Task QueueDirectNotification(NotificationEvent notificationMessage, NotificationChannel directChannel)
            => Task.CompletedTask;

        public Task QueueNotification(NotificationEvent notificationMessage) => Task.CompletedTask;
    }
}
