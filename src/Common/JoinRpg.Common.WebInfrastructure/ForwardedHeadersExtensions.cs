using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JoinRpg.Common.WebInfrastructure;

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

    /// <summary>
    /// Настраивает ForwardedHeaders и сам включает их в начало конвейера (через <see cref="IStartupFilter"/>),
    /// приложению вызывать <c>UseForwardedHeaders()</c> не нужно.
    /// Вызывается из <c>WebInfrastructureRegistration.AddJoinWebPlatform</c> (всех перегрузок).
    /// </summary>
    /// <remarks>
    /// Сразу за ForwardedHeaders считается метрика <see cref="PrivateNetworkRequestMetrics"/> —
    /// она ловит неправильную настройку ForwardedHeaders.
    /// </remarks>
    internal static IServiceCollection AddJoinForwardedHeaders(this IServiceCollection services)
    {
        _ = services.ConfigureForwardedHeaders();
        services.TryAddSingleton<PrivateNetworkRequestMetrics>();
        services.TryAddEnumerable(ServiceDescriptor.Transient<IStartupFilter, ForwardedHeadersStartupFilter>());
        return services;
    }

    private sealed class ForwardedHeadersStartupFilter(PrivateNetworkRequestMetrics privateNetworkMetrics) : IStartupFilter
    {
        /// <summary>
        /// Ключ в <see cref="HttpContext.Items"/>: заголовки X-Forwarded-* до ForwardedHeadersMiddleware
        /// (<see cref="IncomingForwardedHeaders"/>).
        /// </summary>
        private static readonly object IncomingForwardedHeadersKey = new();

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            _ = app.Use((context, nextMiddleware) =>
            {
                context.Items[IncomingForwardedHeadersKey] = IncomingForwardedHeaders.Capture(context.Request);
                return nextMiddleware(context);
            });

            _ = app.UseForwardedHeaders();

            _ = app.Use((context, nextMiddleware) =>
            {
                var incoming = context.Items[IncomingForwardedHeadersKey] as IncomingForwardedHeaders? ?? IncomingForwardedHeaders.None;
                privateNetworkMetrics.Record(context, incoming);
                return nextMiddleware(context);
            });

            next(app);
        };
    }
}
