using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace JoinRpg.Common.WebComponentBook.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        builder.Services.AddSingleton<IUserLinkResolveClient, DemoUserLinkResolveClient>();
        builder.Services.AddSingleton<IUriLocator<UserLinkViewModel>, DemoUserLinkLocator>();

        await builder.Build().RunAsync();
    }
}
