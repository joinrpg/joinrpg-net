using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JoinRpg.Common.BastiliaRatingClient;

public static class Registration
{
    public static void AddBastiliaRatingClient(this IServiceCollection services)
    {
        _ = services.AddOptions<BastiliaRatingOptions>();
        _ = services.AddHttpClient<IBastiliaRatingClient, BastiliaRatingClient>(
            (s, c) => c.BaseAddress = s.GetRequiredService<IOptions<BastiliaRatingOptions>>().Value.HostName);
    }
}
