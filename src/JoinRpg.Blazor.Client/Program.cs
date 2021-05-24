using System;
using System.Net.Http;
using System.Threading.Tasks;
using JoinRpg.Web.GameSubscribe;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

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
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.Services
                .AddHttpClient<IGameSubscribeClient, ApiClients.GameSubscribeClient>(
                    client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
                );
            //.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

            // builder.Services.AddApiAuthorization();

            await builder.Build().RunAsync();
        }
    }
}
