using JoinRpg.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JoinRpg.Integrations.KogdaIgra;
public static class Registration
{
    public static void AddKogdaIgra(this IJoinServiceCollection services)
    {
        _ = services.AddOptions<KogdaIgraOptions>();
        _ = services.AddHttpClient<IKogdaIgraApiClient, KogdaIgraApiClient>(
            (s, c) => c.BaseAddress = s.GetRequiredService<IOptions<KogdaIgraOptions>>().Value.HostName);

        _ = services
            .AddTransient<IKogdaIgraSyncService, KogdaIgraSyncService>()
            .AddTransient<IKogdaIgraBindService, KogdaIgraSyncService>();

        services.AddDailyJob<SyncKogdaIgraJob>();
    }
}
