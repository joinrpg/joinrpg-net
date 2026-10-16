using JoinRpg.Common.WebInfrastructure.DataProtection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace JoinRpg.Common.WebInfrastructure;

public static class DataProtectionRegistration
{
    public static void AddJoinDataProtection(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment environment, string appName, string connectionStringName)
    {
        var dataProtection = services.AddDataProtection().SetApplicationName(appName);

        if (services.AddJoinEfCoreDbContext<DataProtectionDbContext>(configuration, environment, connectionStringName))
        {
            dataProtection.PersistKeysToDbContext<DataProtectionDbContext>();
        }
    }

    /// <summary>
    /// Хранить ключи в DbContext самого приложения, без отдельной базы для ключей.
    /// Контекст регистрируется отдельно (например, через <see cref="DbContextRegisterHelper.AddJoinEfCoreDbContext"/>),
    /// таблица ключей появляется в его миграциях.
    /// </summary>
    public static void AddJoinDataProtection<TContext>(this IServiceCollection services, string appName)
        where TContext : DbContext, IDataProtectionKeyContext
        => services.AddDataProtection().SetApplicationName(appName).PersistKeysToDbContext<TContext>();
}
