using JoinRpg.Common.KogdaIgraClient;
using JoinRpg.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Integrations.KogdaIgra;

public static class Registration
{
    public static void AddKogdaIgra(this IJoinServiceCollection services)
    {
        services.AddKogdaIgraClient();

        _ = services
            .AddTransient<IKogdaIgraSyncService, KogdaIgraSyncService>()
            .AddTransient<IKogdaIgraBindService, KogdaIgraSyncService>()
            .AddTransient<IKogdaIgraInfoService, KogdaIgraSyncService>()
            ;
        services.AddDailyJob<SyncKogdaIgraJob>();
    }
}
