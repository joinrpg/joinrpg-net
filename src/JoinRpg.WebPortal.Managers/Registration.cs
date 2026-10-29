using JoinRpg.Common.WebComponents;
using JoinRpg.Web.Accommodation;
using JoinRpg.Web.Accommodation.Rooms;
using JoinRpg.Web.AdminTools;
using JoinRpg.Web.AdminTools.Notifications;
using JoinRpg.Web.CharacterGroups.GroupReport;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;
using JoinRpg.Web.CheckIn;
using JoinRpg.Web.Claims;
using JoinRpg.Web.Claims.Finance;
using JoinRpg.Web.Claims.UnifiedGrid;
using JoinRpg.Web.Games.Projects;
using JoinRpg.Web.Plots;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Claims;
using JoinRpg.Web.ProjectCommon.Fields;
using JoinRpg.Web.ProjectCommon.KogdaIgra;
using JoinRpg.Web.ProjectCommon.Projects;
using JoinRpg.Web.ProjectMasterTools.Acl;
using JoinRpg.Web.ProjectMasterTools.CaptainRules;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using JoinRpg.Web.ProjectMasterTools.Fields;
using JoinRpg.Web.ProjectMasterTools.ProjectRolesLists;
using JoinRpg.Web.ProjectMasterTools.ResponsibleMaster;
using JoinRpg.Web.ProjectMasterTools.Settings;
using JoinRpg.Web.ProjectMasterTools.Subscribe;
using JoinRpg.WebPortal.Managers.Characters;
using JoinRpg.WebPortal.Managers.CheckIn;
using JoinRpg.WebPortal.Managers.Claims;
using JoinRpg.WebPortal.Managers.ProjectMasterTools.ProjectRolesLists;
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
        .AddScoped<IMasterProfileClient, ProjectMasterTools.MasterProfileViewService>()
        .AddScoped<IMasterAccessClient, ProjectMasterTools.MasterAccessViewService>()
        .AddScoped<IProjectInfoClient, ProjectInfoViewService>()
        .AddScoped<FieldSetupManager>()
        .AddScoped<Schedule.SchedulePageManager>()
        .AddScoped<IGameSubscribeClient, SubscribeViewService>()
        .AddScoped<ICharacterGroupsClient, CharacterGroupList.CharacteGroupListViewService>()
        .AddScoped<ICharactersClient, CharacterGroupList.CharacterListViewService>()
        .AddScoped<ICheckInClient, CheckInViewService>()
        .AddScoped<CheckInDeskManager>()
        .AddScoped<CheckInClaimViewModelBuilder>()
        .AddScoped<IResponsibleMasterRuleClient, ProjectMasterTools.ResponsibleMasterRules.ResponsibleMasterRuleViewService>()
        .AddScoped<ICaptainRuleClient, ProjectMasterTools.CaptainRules.CaptainRuleViewService>()
        .AddScoped<IProjectRolesListClient, ProjectRolesListViewService>()
        .AddScoped<Web.ProjectMasterTools.Badges.IBadgeTemplateClient, ProjectMasterTools.Badges.BadgeTemplateViewService>()
        .AddScoped<IProjectRoleGridClient, CharacterGroups.ProjectRoleGridViewService>()
        .AddScoped<IGroupReportClient, CharacterGroups.GroupReportViewService>()
        .AddScoped<IProjectFieldsClient, Fields.ProjectFieldsViewService>()
        .AddScoped<IMasterClient, ProjectMasterViewService>()
        .AddTransient<IProjectListClient, ProjectListViewService>()
        .AddTransient<IProjectListForAdminClient, ProjectListViewService>()
        .AddScoped<IProjectCreateClient, ProjectCreateViewService>()
        .AddScoped<IPlotClient, Plots.PlotViewService>()
        .AddScoped<IKogdaIgraSyncClient, AdminTools.KogdaIgraSyncManager>()
        .AddScoped<IKogdaIgraBindClient, AdminTools.KogdaIgraSyncManager>()
        .AddScoped<Plots.CharacterPlotViewService>()
        .AddScoped<Print.PrintViewService>()
        .AddScoped<Print.BadgePrintService>()
        .AddScoped<IAccommodationInviteClient, Accommodation.AccommodationInviteViewService>()
        .AddScoped<IAccommodationTypeClient, Accommodation.AccommodationTypeViewService>()
        .AddScoped<IRoomTypeEditClient, Accommodation.RoomTypeEditViewService>()
        .AddScoped<Accommodation.RoomTypeRoomsViewService>()
        .AddScoped<IAccommodationRoomsClient>(sp => sp.GetRequiredService<Accommodation.RoomTypeRoomsViewService>())
        .AddScoped<IClaimOperationClient, ClaimsViewService>()
        .AddScoped<IClaimListClient, ClaimsViewService>()
        .AddScoped<IUnifiedGridClient, UnifiedGridViewService>()
        .AddScoped<IInvitePlayerClient, InvitePlayerViewService>()
        .AddScoped<IClaimRestoreClient, Drp.ClaimRestoreViewService>()
        .AddScoped<ICommentRestoreClient, Drp.CommentRestoreViewService>()
        .AddScoped<IFinanceRestoreClient, Drp.FinanceRestoreViewService>()
        .AddScoped<IMoneyTransferClient, MoneyTransferViewService>()
        .AddScoped<IFinanceModerationClient, FinanceModerationViewService>()
        .AddScoped<JoinRpg.Web.Claims.Comments.IClaimCommentClient, ClaimCommentViewService>()
        .AddScoped<Comments.CommentDiscussionOwnerLoader>()
        .AddScoped<Comments.DiscussionViewModelBuilder>()
        .AddScoped<IUserLinkResolver, UserLinkResolver>()
        .AddScoped<IUserLinkResolveClient, UserLinkResolveViewService>()
        .AddScoped<IMoveClient, MoveViewService>()
        .AddScoped<IProjectFieldOperationsClient, Fields.ProjectFieldOperationsViewService>()
        .AddScoped<INotificationDashboardClient, AdminTools.NotificationDashboardManager>()
        .AddScoped<INotificationLogClient, AdminTools.NotificationLogManager>()
        .AddScoped<JoinRpg.Web.AdminTools.UserMerge.IUserMergeClient, AdminTools.UserMergeViewService>()
        .AddScoped<AdminTools.PscbPaymentsViewService>()
        // Шлюз списка и восстановление (ADR027) делят один экземпляр: восстановление переиспользует
        // скачивание и обогащение списка, а не ходит в банк второй раз.
        .AddScoped<JoinRpg.Web.AdminTools.PscbPayments.IPscbPaymentsClient>(sp => sp.GetRequiredService<AdminTools.PscbPaymentsViewService>())
        .AddScoped<JoinRpg.Web.AdminTools.PscbPayments.IPscbPaymentsRestoreClient, AdminTools.PscbPaymentsRestoreViewService>()
        .AddScoped<ICharacterApiViewService, CharacterApiViewService>()
        .AddScoped<IProjectApiViewService, ProjectApiViewService>()
        .AddScoped<ISearchApiViewService, SearchApiViewService>()

        ;
    }
}
