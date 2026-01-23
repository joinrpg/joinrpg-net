using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Areas.Admin.Controllers;

[AdminAuthorize]
[Area("Admin")]
public class DumpConfigController : Controller
{
    public ContentHttpResult Index([FromServices] IConfiguration configuration)
    {
        var config = ((IConfigurationRoot)configuration).GetDebugView();
        return TypedResults.Text(config);
    }

    public ContentHttpResult Endpoints([FromServices] EndpointDataSource endpointDataSource)
    {
        var content = string.Join("\n\n", endpointDataSource.Endpoints.Select(e => DisplayEndpoint(e)));
        return TypedResults.Text(content);
    }

    private static string? DisplayEndpoint(Endpoint e) =>
        e switch
        {
            RouteEndpoint routeEndpoint => $"Route: {routeEndpoint.RoutePattern.RawText} → {routeEndpoint.DisplayName}",
            _ => e.ToString()
        };

}
