using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace JoinRpg.Portal.Infrastructure;

public static class DataProtectionRegistration
{
    public static void AddJoinDataProtection(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var dataProtection = services.AddDataProtection();
        var dataProtectionConnectionString = configuration.GetConnectionString("DataProtection");

        if (!environment.IsDevelopment() && !string.IsNullOrWhiteSpace(dataProtectionConnectionString))
        {
            services.AddJoinEfCoreDbContext<DataProtectionDbContext>(configuration, environment, "DataProtection");
            dataProtection.PersistKeysToDbContext<DataProtectionDbContext>();
        }
    }
}
