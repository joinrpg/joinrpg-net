using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Interfaces;

public interface IJoinServiceCollection : IServiceCollection
{
    IJoinServiceCollection AddDailyJob<TJob>() where TJob : class, IDailyJob;
}
