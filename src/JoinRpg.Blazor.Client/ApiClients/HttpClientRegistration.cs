using JoinRpg.Web.AdminTools.KogdaIgra;
using JoinRpg.Web.CheckIn;
using JoinRpg.Web.Claims;
using JoinRpg.Web.Games.Projects;
using JoinRpg.Web.Plots;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Claims;
using JoinRpg.Web.ProjectCommon.Projects;
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
                .AddHttpClient<IProjectCreateClient, ProjectCreateClient>()
                .AddHttpClient<IProjectSettingsClient, ProjectSettingsClient>()
                .AddHttpClient<IPlotClient, PlotClient>()
                .AddHttpClient<IMasterClient, MasterClient>()
                .AddHttpClient<IKogdaIgraSyncClient, KogdaIgraClient>()
                .AddHttpClient<IKogdaIgraBindClient, KogdaIgraClient>()
                .AddHttpClient<IGameSubscribeClient, GameSubscribeClient>()
                .AddHttpClient<ICharacterGroupsClient, CharacterGroupsClient>()
                .AddHttpClient<ICharactersClient, CharactersClient>()
                .AddHttpClient<ICheckInClient, CheckInClient>()
                .AddHttpClient<IClaimOperationClient, ClaimHttpClient>()
                .AddHttpClient<IClaimListClient, ClaimHttpClient>()
                .AddHttpClient<IResponsibleMasterRuleClient, ResponsibleMasterRuleClient>();
    }
}
