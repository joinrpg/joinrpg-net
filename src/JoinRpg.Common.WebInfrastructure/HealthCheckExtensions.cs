using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;

namespace JoinRpg.Common.WebInfrastructure;

public static class HealthCheckExtensions
{
    public static void MapJoinHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapHealthChecks("/health",
            new HealthCheckOptions { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse })
            .WithMetadata(new AllowAnonymousAttribute());
        _ = endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions()
        {
            Predicate = (check) => check.Tags.Contains("ready"),
        }).WithMetadata(new AllowAnonymousAttribute());

        _ = endpoints.MapHealthChecks("/health/live", new HealthCheckOptions()
        {
            Predicate = (_) => false
        }).WithMetadata(new AllowAnonymousAttribute());
    }
}
