using JoinRpg.Web.CheckIn;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectMasterTools.ResponsibleMaster;
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
                .AddHttpClient<IGameSubscribeClient, GameSubscribeClient>()
                .AddHttpClient<ICharacterGroupsClient, CharacterGroupsClient>()
                .AddHttpClient<ICheckInClient, CheckInClient>()
                .AddHttpClient<IResponsibleMasterRuleClient, ResponsibleMasterRuleClient>();
    }
}
