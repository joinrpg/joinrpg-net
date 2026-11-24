using JoinRpg.Common.WebInfrastructure.Job;
using JoinRpg.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Services.Advertisement;

public static class AdvertisementRegistration
{
    public static IJoinServiceCollection AddJoinAdvertisement(this IJoinServiceCollection services)
    {
        _ = services
            .AddScoped<IAdvSenderFactory, AdvSenderFactory>();
        services.AddJob<AdvertisementJob>();
        return services;
    }
}
