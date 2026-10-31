using JoinRpg.Web.Accommodation;
using JoinRpg.Web.Accommodation.Rooms;
using JoinRpg.Web.AdminTools;
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
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace JoinRpg.Blazor.Client.ApiClients;

public static class HttpClientRegistration
{
    private static WebAssemblyHostBuilder AddHttpClient<TClient, TImplementation>(
        this WebAssemblyHostBuilder builder)

        where TClient : class
        where TImplementation : class, TClient
    {
        _ = builder.Services.AddHttpClient<TClient, TImplementation>(
            client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
        return builder;
    }

    public static WebAssemblyHostBuilder AddHttpClients(this WebAssemblyHostBuilder builder)
    {
        return builder
                .AddHttpClient<IProjectInfoClient, ProjectInfoClient>()
                .AddHttpClient<IProjectListClient, ProjectListClient>()
                .AddHttpClient<IProjectListForAdminClient, ProjectListClient>()
                .AddHttpClient<IProjectCreateClient, ProjectCreateClient>()
                .AddHttpClient<IProjectSettingsClient, ProjectSettingsClient>()
                .AddHttpClient<JoinRpg.Web.ProjectMasterTools.Finance.IFinanceSettingsClient, FinanceSettingsClient>()
                .AddHttpClient<IMasterProfileClient, MasterProfileClient>()
                .AddHttpClient<IMasterAccessClient, MasterAccessClient>()
                .AddHttpClient<IPlotClient, PlotClient>()
                .AddHttpClient<IMasterClient, MasterClient>()
                .AddHttpClient<IKogdaIgraSyncClient, KogdaIgraClient>()
                .AddHttpClient<IKogdaIgraBindClient, KogdaIgraClient>()
                .AddHttpClient<IGameSubscribeClient, GameSubscribeClient>()
                .AddHttpClient<ICharacterGroupsClient, CharacterGroupsClient>()
                .AddHttpClient<ICharactersClient, CharactersClient>()
                .AddHttpClient<ICheckInClient, CheckInClient>()
                .AddHttpClient<IAccommodationInviteClient, AccommodationInviteClient>()
                .AddHttpClient<IAccommodationTypeClient, AccommodationInviteClient>()
                .AddHttpClient<IRoomTypeEditClient, RoomTypeEditClient>()
                .AddHttpClient<IAccommodationRoomsClient, AccommodationRoomsClient>()
                .AddHttpClient<IClaimOperationClient, ClaimHttpClient>()
                .AddHttpClient<IClaimListClient, ClaimHttpClient>()
                .AddHttpClient<IUnifiedGridClient, ClaimHttpClient>()
                .AddHttpClient<ICaptainRuleClient, CaptainRuleClientImpl>()
                .AddHttpClient<IProjectFieldsClient, ProjectFieldsClient>()
                .AddHttpClient<IProjectRolesListClient, ProjectRolesListClientImpl>()
                .AddHttpClient<JoinRpg.Web.ProjectMasterTools.Badges.IBadgeTemplateClient, BadgeTemplateClientImpl>()
                .AddHttpClient<IProjectRoleGridClient, ProjectRoleGridClientImpl>()
                .AddHttpClient<IGroupReportClient, GroupReportClientImpl>()
                .AddHttpClient<IResponsibleMasterRuleClient, ResponsibleMasterRuleClient>()
                .AddHttpClient<IInvitePlayerClient, InvitePlayerClient>()
                .AddHttpClient<IClaimRestoreClient, ClaimRestoreClient>()
                .AddHttpClient<ICommentRestoreClient, CommentRestoreClient>()
                .AddHttpClient<IFinanceRestoreClient, FinanceRestoreClient>()
                .AddHttpClient<IMoneyTransferClient, MoneyTransferClient>()
                .AddHttpClient<IFinanceModerationClient, FinanceModerationClient>()
                .AddHttpClient<JoinRpg.Web.Claims.Comments.IClaimCommentClient, ClaimCommentClient>()
                .AddHttpClient<IMoveClient, MoveClientImpl>()
                .AddHttpClient<IUserLinkResolveClient, UserLinkResolveClientImpl>()
                .AddHttpClient<IProjectFieldOperationsClient, ProjectFieldOperationsClientImpl>()
                .AddHttpClient<IUserAdminClient, UserAdminHttpClient>()
                .AddHttpClient<JoinRpg.Web.AdminTools.UserMerge.IUserMergeClient, UserMergeHttpClient>()
                .AddHttpClient<JoinRpg.Web.AdminTools.PscbPayments.IPscbPaymentsClient, PscbPaymentsHttpClient>()
                .AddHttpClient<JoinRpg.Web.AdminTools.PscbPayments.IPscbPaymentsRestoreClient, PscbPaymentsRestoreHttpClient>();
    }
}
