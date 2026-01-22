using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;

namespace Joinrpg.Web.Identity;

public static class ForwardedHeadersExtensions
{

    public static IServiceCollection ConfigureForwardedHeaders(this IServiceCollection services)
    {
        return services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            options.ForwardLimit = 1;
            // Allow nearest proxy server to set X-Forwarded-?? header
            // Do not white-list servers (It's hard to know them specifically proxy server in cloud)
            // It will allow IP-spoofing, if Kestrel is directly exposed to end user
            // But it should never happen anyway (we always should be under at least one proxy)
        });
    }
}
