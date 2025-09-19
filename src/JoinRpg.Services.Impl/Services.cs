using System.Reflection;
using JoinRpg.Interfaces;
using JoinRpg.Services.Impl.Notifications;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Impl.Search;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Avatars;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Services.Impl;

public static class Services
{
    public static IEnumerable<Type> GetTypes()
    {
        //TODO переместить все отсюда в AddJoinDomainServices
        yield return typeof(ProjectService);
        yield return typeof(CreateProjectService);
        yield return typeof(ClaimServiceImpl);
        yield return typeof(SearchServiceImpl);
        yield return typeof(PlotServiceImpl);
        yield return typeof(FinanceOperationsImpl);
        yield return typeof(ForumServiceImpl);
        yield return typeof(FieldSetupServiceImpl);
        yield return typeof(FieldDefaultValueGenerator);
        yield return typeof(AccommodationInviteServiceImpl);
        yield return typeof(AccommodationServiceImpl);
        yield return typeof(CharacterServiceImpl);
        yield return typeof(GameSubscribeService);
        yield return typeof(RespMasterRuleService);

        yield return typeof(ProjectAccessService);

        yield return typeof(CloneProjectHelperFactory);

        foreach (var provider in Assembly.GetExecutingAssembly().DefinedTypes.Where(t => t.IsAssignableTo(typeof(ISearchProvider))).Select(t => t.AsType()))
        {
            yield return provider;
        }
    }

    public static IJoinServiceCollection AddJoinDomainServices(this IJoinServiceCollection services)
    {
        return
            services
            .AddDailyJob<DailyChangedPlayerClaimsNotificationJob>()
            .AddDailyJob<ProjectPerformCloseJob>();
    }

    [Obsolete("После того, как IdPortal заработает, убрать отсюда UserService в отдельную сборку в принципе")]
    public static IServiceCollection AddUserServicesOnly(this IServiceCollection services)
    {
        return services
            .AddTransient<IUserService, UserServiceImpl>()
            .AddTransient<IAvatarService, UserServiceImpl>();
    }
}
