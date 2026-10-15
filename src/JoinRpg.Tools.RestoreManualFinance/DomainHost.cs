using System.Runtime.CompilerServices;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Dal.Notifications;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Interfaces;
using JoinRpg.Portal;
using JoinRpg.Services.Drp.ClaimNotifications;
using JoinRpg.Services.Interfaces.Drp;
using JoinRpg.Services.Interfaces.Projects;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace JoinRpg.Tools.RestoreManualFinance;

/// <summary>
/// DI портала без HTTP и без фоновых служб — чтобы читать и восстанавливать тем же доменным кодом, что и
/// сайт. Основная БД и база уведомлений — из настроек DRP, а не из appsettings портала.
/// Каждая операция — в своём scope: метаданные проекта кешируются на scope, а созданный тип «наличные»
/// должен быть виден следующему вызову.
/// </summary>
internal sealed class DomainHost : IManualFinanceDomain, IAsyncDisposable
{
    private readonly WebApplicationFactory<Startup> factory;

    public DomainHost(string mssqlConnectionString, string notificationsConnectionString)
    {
        factory = new WebApplicationFactory<Startup>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseContentRoot(GetPortalContentRoot());
                builder.UseSetting("ConnectionStrings:DefaultConnection", mssqlConnectionString);
                builder.UseSetting("ConnectionStrings:Notifications", notificationsConnectionString);
                // В консоль — только количества и id: журнал портала пишет аргументы и тексты
                // (а при ошибке — со стеком). Причина ошибки попадает в отчёт.
                builder.UseSetting("Logging:LogLevel:Default", "Error");
                builder.UseSetting("Logging:LogLevel:Microsoft", "Error");
                builder.ConfigureTestServices(services => services.RemoveAll<IHostedService>());
            });

        var actualMssql = factory.Services.GetRequiredService<IJoinDbContextConfiguration>().ConnectionString;
        if (actualMssql != mssqlConnectionString)
        {
            throw new InvalidOperationException("Доменный слой подключился бы не к той основной БД, что задана в настройках DRP. Прерываю.");
        }

        using var scope = factory.Services.CreateScope();
        var actualNotifications = scope.ServiceProvider.GetRequiredService<NotificationsDataDbContext>().Database.GetConnectionString();
        if (PostgresTarget(actualNotifications) != PostgresTarget(notificationsConnectionString))
        {
            throw new InvalidOperationException("Доменный слой подключился бы не к той базе уведомлений, что задана в настройках DRP. Прерываю.");
        }
    }

    /// <summary>
    /// Куда ведёт строка Postgres. Целиком строки не сравнить: портал дописывает к ней отключение GSS
    /// (DbContextRegisterHelper), а Npgsql может не отдавать пароль обратно.
    /// </summary>
    private static (string? Host, int Port, string? Database, string? Username) PostgresTarget(string? connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        return (builder.Host, builder.Port, builder.Database, builder.Username);
    }

    public async Task<IReadOnlyList<ProjectIdentification>> GetBlockedProjects()
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IProjectRestoreLookupRepository>().GetBlockedProjects();
    }

    /// <summary>План проекта: уведомления до <paramref name="lostAt"/> и состояние базы.</summary>
    public async Task<ProjectFinancePlan> LoadPlan(ProjectIdentification projectId, DateTimeOffset lostAt, DateTimeOffset backupTakenAt)
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var events = ClaimNotificationParser.Parse(
            await sp.GetRequiredService<INotificationRepository>().GetClaimNotificationsForProject(projectId, lostAt));
        var lookup = await sp.GetRequiredService<IProjectRestoreLookupRepository>()
            .GetClaimFinanceForRestore(projectId, FinanceSourceBuilder.NotifiedCommentIds(events, backupTakenAt));
        return FinanceSourceBuilder.BuildPlan(projectId, events, lookup, backupTakenAt);
    }

    public Task<PaymentTypeIdentification> CreateRestoredCashPaymentType(UserIdentification actingMaster, ProjectIdentification projectId, UserIdentification owner)
        => RunAs(actingMaster, sp => sp.GetRequiredService<IProjectFinanceSettingsService>().CreateRestoredCashPaymentType(projectId, owner));

    public Task<RestoredFinanceResult> RestoreFinanceOperations(
        UserIdentification actingMaster,
        ClaimIdentification claimId,
        IReadOnlyList<RestoredFinanceOperation> operations,
        IReadOnlyList<RestoredFinanceModeration> moderations)
        => RunAs(actingMaster, sp => sp.GetRequiredService<IClaimRestoreService>().RestoreFinanceOperations(claimId, operations, moderations));

    /// <summary>Вызов от имени мастера проекта — как в RestoreProjectShells, в своём scope.</summary>
    private async Task<T> RunAs<T>(UserIdentification userId, Func<IServiceProvider, Task<T>> action)
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var impersonator = sp.GetRequiredService<IImpersonateAccessor>();
        impersonator.StartImpersonate(userId, new UserDisplayName($"Пользователь {userId.Value}", null), IsAdmin: false);
        try
        {
            return await action(sp);
        }
        finally
        {
            impersonator.StopImpersonate();
        }
    }

    public ValueTask DisposeAsync() => factory.DisposeAsync();

    private static string GetPortalContentRoot([CallerFilePath] string sourceFilePath = "")
    {
        // WebApplicationFactory не умеет находить content root Portal при UseArtifactsOutput —
        // берём его из расположения этого файла в дереве исходников (как в JoinRpg.Tools.SeedTestUsers).
        var srcDir = Path.GetDirectoryName(Path.GetDirectoryName(sourceFilePath))!;
        return Path.Combine(srcDir, "JoinRpg.Portal");
    }
}
