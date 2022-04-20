using JoinRpg.Blazor.Client.ApiClients;
using JoinRpg.Web.CharacterGroups;
using JoinRpg.Web.CheckIn;
using JoinRpg.Web.GameSubscribe;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace JoinRpg.Blazor.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            //Workaround for https://github.com/dotnet/aspnetcore/issues/26601
            //If top-level component is not defined in this project,
            //it should be forcefully loaded
            _ = typeof(MasterSubscribeList).ToString();
            _ = typeof(CharacterGroupSelector).ToString();
            _ = typeof(JoinRpg.PrimitiveTypes.CharacterTypeInfo).ToString();
            _ = typeof(CheckInStats).ToString();

            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            AddHttpClient<IGameSubscribeClient, ApiClients.GameSubscribeClient>(builder);
            AddHttpClient<ICharacterGroupsClient, ApiClients.CharacterGroupsClient>(builder);
            AddHttpClient<ICheckInClient, ApiClients.CheckInClient>(builder);

            //.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

            // builder.Services.AddApiAuthorization();

            builder.Services.AddTransient<CsrfTokenProvider>();

            await builder.Build().RunAsync();
        }

        private static IHttpClientBuilder AddHttpClient<TClient, TImplementation>(WebAssemblyHostBuilder builder)
            where TClient : class
            where TImplementation : class, TClient
        => builder.Services.AddHttpClient<TClient, TImplementation>(client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
    }
}
