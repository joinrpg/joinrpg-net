using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;

namespace JoinRpg.Portal.Infrastructure.HealthChecks
{
    internal static class HealthCheckExtensions
    {
        internal static void MapJoinHealthChecks(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHealthChecks("/health").WithMetadata(new AllowAnonymousAttribute());
            endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions()
            {
                Predicate = (check) => check.Tags.Contains("ready"), //TODO m.b. add some probes
            }).WithMetadata(new AllowAnonymousAttribute());

            endpoints.MapHealthChecks("/health/live", new HealthCheckOptions()
            {
                Predicate = (_) => false
            }).WithMetadata(new AllowAnonymousAttribute());
        }
    }
}
