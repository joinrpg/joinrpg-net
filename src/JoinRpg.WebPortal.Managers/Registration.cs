using JoinRpg.Web.AdminTools.KogdaIgra;
using JoinRpg.Web.CheckIn;
using JoinRpg.Web.Claims;
using JoinRpg.Web.Claims.UnifiedGrid;
using JoinRpg.Web.Games.Projects;
using JoinRpg.Web.Plots;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Claims;
using JoinRpg.Web.ProjectCommon.Projects;
using JoinRpg.Web.ProjectMasterTools.CaptainRules;
using JoinRpg.Web.ProjectMasterTools.ResponsibleMaster;
using JoinRpg.Web.ProjectMasterTools.Settings;
using JoinRpg.Web.ProjectMasterTools.Subscribe;
using JoinRpg.WebPortal.Managers.CheckIn;
using JoinRpg.WebPortal.Managers.Claims;
using JoinRpg.WebPortal.Managers.Projects;
using JoinRpg.WebPortal.Managers.Subscribe;
using JoinRpg.WebPortal.Managers.UnifiedGrid;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.WebPortal.Managers;

public static class Registration
{
    public static IServiceCollection AddJoinManagers(this IServiceCollection services)
    {
        return services.AddScoped<ProjectListManager>()
        .AddScoped<IProjectSettingsClient, ProjectSettingsViewService>()
        .AddScoped<IProjectInfoClient, ProjectInfoViewService>()
        .AddScoped<FieldSetupManager>()
        .AddScoped<Schedule.SchedulePageManager>()
        .AddScoped<IGameSubscribeClient, SubscribeViewService>()
        .AddScoped<ICharacterGroupsClient, CharacterGroupList.CharacteGroupListViewService>()
        .AddScoped<ICharactersClient, CharacterGroupList.CharacterListViewService>()
        .AddScoped<ICheckInClient, CheckInViewService>()
        .AddScoped<IResponsibleMasterRuleClient, ProjectMasterTools.ResponsibleMasterRules.ResponsibleMasterRuleViewService>()
        .AddScoped<ICaptainRuleClient, ProjectMasterTools.CaptainRules.CaptainRuleViewService>()
        .AddScoped<IMasterClient, ProjectMasterViewService>()
        .AddScoped<IProjectListClient, ProjectListViewService>()
        .AddScoped<MassMailManager>()
        .AddScoped<IPlotClient, Plots.PlotViewService>()
        .AddScoped<IKogdaIgraSyncClient, AdminTools.KogdaIgraSyncManager>()
        .AddScoped<IKogdaIgraBindClient, AdminTools.KogdaIgraSyncManager>()
        .AddScoped<Plots.CharacterPlotViewService>()
        .AddScoped<IClaimOperationClient, ClaimsViewService>()
        .AddScoped<IClaimListClient, ClaimsViewService>()
        .AddScoped<IUnifiedGridClient, UnifiedGridViewService>()

        ;
    }
}
