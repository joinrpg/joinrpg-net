using JoinRpg.Common.WebInfrastructure.DataProtection;
using JoinRpg.Dal.CommonEfCore;
using Microsoft.AspNetCore.DataProtection;
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
}
