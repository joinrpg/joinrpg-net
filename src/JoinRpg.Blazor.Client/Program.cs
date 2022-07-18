using JoinRpg.Blazor.Client.ApiClients;
using JoinRpg.Web.CharacterGroups;
using JoinRpg.Web.CheckIn;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectMasterTools.ResponsibleMaster;
using JoinRpg.Web.ProjectMasterTools.Subscribe;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace JoinRpg.Blazor.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        //Workaround for https://github.com/dotnet/aspnetcore/issues/26601
        //If top-level component is not defined in this project,
        //it should be forcefully loaded
        _ = typeof(MasterSubscribeList).ToString();
        _ = typeof(CharacterGroupSelector).ToString();
        _ = typeof(CharacterTypeSelector).ToString();
        _ = typeof(PrimitiveTypes.CharacterTypeInfo).ToString();
        _ = typeof(CheckInStats).ToString();
        _ = typeof(ResponsibleMasterRulesList).ToString();

        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.AddHttpClients();

        //.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

        // builder.Services.AddApiAuthorization();

        builder.Services.AddTransient<CsrfTokenProvider>();

        builder.Services.AddUriLocator();

        await builder.Build().RunAsync();
    }


}
