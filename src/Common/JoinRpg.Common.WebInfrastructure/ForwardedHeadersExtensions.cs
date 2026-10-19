using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace JoinRpg.Common.WebInfrastructure;

public static class ForwardedHeadersExtensions
{
    /// <summary>
    /// Срок HSTS. Пока небольшой: если с https что-то пойдёт не так, браузеры быстро забудут политику.
    /// Поднимать (до года), когда редирект на https обкатается на проде.
    /// </summary>
    internal static readonly TimeSpan HstsMaxAge = TimeSpan.FromDays(1);

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
    /// <para>
    /// Сразу за ForwardedHeaders считается метрика <see cref="PrivateNetworkRequestMetrics"/> —
    /// она ловит неправильную настройку ForwardedHeaders.
    /// </para>
    /// <para>
    /// Затем — редирект http → https (307) и HSTS (вне Development). Схему запроса знаем из X-Forwarded-Proto,
    /// поэтому редирект стоит после ForwardedHeaders. Служебные пути (/health*, /metrics) не редиректятся:
    /// kubelet и Prometheus ходят на под по http. Отключается настройкой <see cref="JoinHttpsRedirectionOptions.Enabled"/>.
    /// </para>
    /// </remarks>
    internal static IServiceCollection AddJoinForwardedHeaders(this IServiceCollection services, IWebHostEnvironment environment)
    {
        _ = services.ConfigureForwardedHeaders();

        _ = services.AddOptions<JoinHttpsRedirectionOptions>().BindConfiguration(JoinHttpsRedirectionOptions.SectionName);

        _ = services.AddHttpsRedirection(options =>
        {
            options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
            if (!environment.IsDevelopment())
            {
                // Kestrel слушает только http за прокси, сам https-порт middleware не определит
                // и молча ничего не будет делать. В Development порт берётся из адресов Kestrel.
                options.HttpsPort = 443;
            }
        });

        _ = services.AddHsts(options =>
        {
            options.MaxAge = HstsMaxAge;
            options.IncludeSubDomains = false;
            options.Preload = false;
        });

        services.TryAddSingleton<PrivateNetworkRequestMetrics>();
        services.TryAddEnumerable(ServiceDescriptor.Transient<IStartupFilter, ForwardedHeadersStartupFilter>());
        return services;
    }

    private sealed class ForwardedHeadersStartupFilter(
        PrivateNetworkRequestMetrics privateNetworkMetrics,
        IWebHostEnvironment environment,
        IOptions<JoinHttpsRedirectionOptions> httpsRedirectionOptions) : IStartupFilter
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

            if (httpsRedirectionOptions.Value.Enabled)
            {
                _ = app.UseWhen(
                    context => !InfrastructureEndpoints.IsInfrastructurePath(context.Request.Path),
                    branch => branch.UseHttpsRedirection());

                if (!environment.IsDevelopment())
                {
                    _ = app.UseHsts();
                }
            }

            next(app);
        };
    }
}
