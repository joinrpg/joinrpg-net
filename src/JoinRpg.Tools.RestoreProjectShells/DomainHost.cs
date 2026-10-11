using System.Runtime.CompilerServices;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Interfaces;
using JoinRpg.Portal;
using JoinRpg.Services.Interfaces.Projects;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JoinRpg.Tools.RestoreProjectShells;

/// <summary>
/// DI портала без HTTP и без фоновых служб — чтобы создать проект тем же доменным кодом, что и сайт.
/// Основная БД — из настроек DRP, а не из appsettings портала.
/// </summary>
internal sealed class DomainHost : IAsyncDisposable
{
    private readonly WebApplicationFactory<Startup> factory;

    public DomainHost(string mssqlConnectionString)
    {
        factory = new WebApplicationFactory<Startup>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseContentRoot(GetPortalContentRoot());
                builder.UseSetting("ConnectionStrings:DefaultConnection", mssqlConnectionString);
                // В консоль — только количества и id: журнал портала пишет название и аргументы
                // (а при ошибке — со стеком). Причина ошибки попадает в отчёт.
                builder.UseSetting("Logging:LogLevel:Default", "Error");
                builder.UseSetting("Logging:LogLevel:Microsoft", "Error");
                builder.ConfigureTestServices(services => services.RemoveAll<IHostedService>());
            });

        var actual = factory.Services.GetRequiredService<IJoinDbContextConfiguration>().ConnectionString;
        if (actual != mssqlConnectionString)
        {
            throw new InvalidOperationException("Доменный слой подключился бы не к той основной БД, что задана в настройках DRP. Прерываю.");
        }
    }

    /// <summary>
    /// Создаёт оболочку проекта от имени создателя.
    /// </summary>
    /// <returns>Id, который получил проект.</returns>
    public async Task<int> CreateShell(int creatorUserId, string name)
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var impersonator = sp.GetRequiredService<IImpersonateAccessor>();
        impersonator.StartImpersonate(
            new UserIdentification(creatorUserId),
            new UserDisplayName($"Пользователь {creatorUserId}", null),
            IsAdmin: false);
        try
        {
            var projectId = await sp.GetRequiredService<IProjectService>().AddRestoredProjectShell(new ProjectName(name));
            return projectId.Value;
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
