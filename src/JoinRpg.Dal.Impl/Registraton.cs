using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Dal.Impl.Repositories.Accommodation;
using JoinRpg.Dal.Impl.Repositories.Characters;
using JoinRpg.Dal.Impl.Repositories.ProjectMetadata;
using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.Data.Interfaces.AdminTools;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Interfaces.Finances;
using JoinRpg.Data.Interfaces.ProjectMetadata;
using JoinRpg.Data.Interfaces.Subscribe;
using JoinRpg.Data.Write.Interfaces;
using Microsoft.Extensions.Configuration;
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
            .AddTransient<ICaptainRulesRepository, CaptainRulesRepository>()
            .AddTransient<IHotCharactersRepository, HotCharactersRepository>()
            .AddTransient<IKogdaIgraRepository, KogdaIgraRepository>()
            .AddTransient<IProjectMetadataRepository, ProjectMetadataRepository>()
            .AddTransient<IProjectMasterProfileRepository, ProjectMasterProfileRepository>()
            .AddTransient<ICharacterGroupRepository, CharacterGroupRepository>()
            .AddTransient<IProjectRepository, ProjectRepository>()
            .AddTransient<ICharacterRepository, CharacterRepositoryImpl>()
            .AddTransient<ICharacterInfoRepository, CharacterInfoRepository>()
            .AddTransient<IClaimInfoRepository, ClaimInfoRepository>()
            // ICharacterAggregateWriteRepository здесь НЕ регистрируется намеренно (ADR014):
            // MyDbContext транзиентен, поэтому DI-экземпляр репозитория получил бы другой
            // DbContext — мутация трекалась бы в одном, а SaveChanges шёл бы в другом.
            // Единственный способ его получить — IUnitOfWork.GetCharacterAggregateWriteRepository().
            .AddTransient<IPlotRepository, PlotRepositoryImpl>()
            .AddTransient<IForumRepository, ForumRepositoryImpl>()
            .AddTransient<IFinanceReportRepository, FinanceReportRepositoryImpl>()
            .AddTransient<IClaimsRepository, ClaimsRepositoryImpl>()
            .AddTransient<IAccommodationInviteRepository, AccommodationInviteRepositoryImpl>()
            .AddTransient<IAccommodationRepository, AccommodationRepositoryImpl>()
            .AddTransient<IAccommodationRequestRepository, AccommodationRequestRepositoryImpl>()
            .AddTransient<IRoomCategoryPlanRepository, RoomCategoryPlanRepository>()
            // IRoomCategoryPlanWriteRepository здесь НЕ регистрируется намеренно (ADR018), по той
            // же причине, что и ICharacterAggregateWriteRepository выше. Единственный способ его
            // получить — IUnitOfWork.GetRoomCategoryPlanWriteRepository().
            .AddTransient<IProjectRolesListRepository, ProjectRolesListRepository>()
            .AddTransient<IFinanceOperationsRepository, FinanceOperationsRepository>()
            .AddTransient<IAdvertisementLogRepository, AdvertisementLogRepository>()
            .AddSingleton<IJoinDbContextConfiguration, ConfigurationAdapter>();

        return services;
    }
}

internal class ConfigurationAdapter(IConfiguration configuration) : Dal.Impl.IJoinDbContextConfiguration
{
    public string ConnectionString => configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}
