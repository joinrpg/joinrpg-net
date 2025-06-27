using System.Reflection;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Dal.Impl;

public static class Registraton
{
    public static IServiceCollection AddJoinDal(this IServiceCollection services)
    {
        _ = services
            .AddTransient<MyDbContext>()
            .AddTransient<IUnitOfWork, MyDbContext>()
            .AddTransient<IUserRepository, UserInfoRepository>()
            .AddTransient<IUserSubscribeRepository, UserInfoRepository>()
            .AddTransient<IProjectMetadataRepository, ProjectMetadataRepository>();

        foreach (var type in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsAssignableTo(typeof(RepositoryImplBase)) && !t.IsAbstract))
        {
            _ = services.AddTransient(type.GetInterfaces().Where(x => x.FullName!.StartsWith("JoinRpg")).Single(), type);
        }
        return services;
    }
}
