using JoinRpg.Blazor.Client.ApiClients;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace JoinRpg.Blazor.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.AddHttpClients();

        builder.Services.AddTransient<CsrfTokenProvider>();

        builder.Services.AddUriLocator();

        await builder.Build().RunAsync();
    }


}
