using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace JoinRpg.Portal.Infrastructure.HealthChecks
{
    internal static class HealthCheckExtensions
    {
        internal static void MapJoinHealthChecks(this IEndpointRouteBuilder endpoints)
        {
            _ = endpoints.MapHealthChecks("/health",
                new HealthCheckOptions { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse })
                .WithMetadata(new AllowAnonymousAttribute());
            _ = endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions()
            {
                Predicate = (check) => check.Tags.Contains("ready"), //TODO m.b. add some probes
            }).WithMetadata(new AllowAnonymousAttribute());

            _ = endpoints.MapHealthChecks("/health/live", new HealthCheckOptions()
            {
                Predicate = (_) => false
            }).WithMetadata(new AllowAnonymousAttribute());
        }
    }
}
