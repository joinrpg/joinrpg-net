using JoinRpg.Common.WebInfrastructure;
using JoinRpg.IdPortal;
using JoinRpg.IdPortal.Components;
using JoinRpg.IdPortal.OAuthServer;
using Microsoft.Extensions.Options;

var app = WebApplication.CreateBuilder(args).ConfigureServices().Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseRouting();

app.UseJoinRequestLogging();

app.UseAuthentication().UseAuthorization();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(JoinRpg.IdPortal.Client._Imports).Assembly);

app.MapJoinHealthChecks();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Map("/user/{userId:int}", (int userId, IOptions<JoinRpgHostNamesOptions> options) => TypedResults.Redirect("https://" + options.Value.MainHost + "/user/" + userId));

app.MapJoinOAuthServer();

app.Run();

public partial class Program { }
