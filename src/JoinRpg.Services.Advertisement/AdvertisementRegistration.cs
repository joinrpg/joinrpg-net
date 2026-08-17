using JoinRpg.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Services.Advertisement;

public static class AdvertisementRegistration
{
    public static IJoinServiceCollection AddJoinAdvertisement(this IJoinServiceCollection services)
    {
        _ = services
            .AddScoped<IAdvertisementChannelRepository, HardcodedAdvertisementChannelRepository>()
            .AddScoped<IAdvertisementScheduleRepository, HardcodedAdvertisementScheduleRepository>()
            .AddScoped<IAdvertisementLogRepository, NullAdvertisementLogRepository>()
            .AddScoped<IAdvSenderFactory, AdvSenderFactory>();
        services.AddDailyJob<SingleHotRoleAdvertisementJob>();
        return services;
    }
}
