using JoinRpg.Common.KogdaIgraClient;
using JoinRpg.Common.WebInfrastructure.Job;
using JoinRpg.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Integrations.KogdaIgra;

public static class Registration
{
    public static IJoinServiceCollection AddKogdaIgra(this IJoinServiceCollection services)
    {
        services.AddKogdaIgraClient();

        _ = services
            .AddTransient<IKogdaIgraSyncService, KogdaIgraSyncService>()
            .AddTransient<IKogdaIgraBindService, KogdaIgraSyncService>()
            ;
        services.AddJob<SyncKogdaIgraJob>();
        return services;
    }
}
