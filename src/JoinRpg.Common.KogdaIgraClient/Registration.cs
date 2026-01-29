using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JoinRpg.Common.KogdaIgraClient;

public static class Registration
{
    public static void AddKogdaIgraClient(this IServiceCollection services)
    {
        _ = services.AddOptions<KogdaIgraOptions>();
        _ = services.AddHttpClient<IKogdaIgraApiClient, KogdaIgraApiClient>(
            (s, c) => c.BaseAddress = s.GetRequiredService<IOptions<KogdaIgraOptions>>().Value.HostName);
    }
}
