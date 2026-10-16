using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Настройки /robots.txt (секция <c>RobotsTxt</c>).
/// </summary>
public class RobotsTxtOptions
{
    public const string SectionName = "RobotsTxt";

    /// <summary>
    /// Главный хост приложения (можно с портом, как в <see cref="JoinRpgHostNamesOptions"/>).
    /// Индексирование разрешено только на нём, остальные хосты (служебные домены и т.п.) закрыты.
    /// Если в нём нет порта, порт запроса не учитывается.
    /// <c>null</c> или пустая строка — главный хост неизвестен, индексирование разрешено на всех хостах.
    /// </summary>
    public string? MainHost { get; set; }

    /// <summary>
    /// Запретить индексирование целиком, на всех хостах (например, для dev-окружения).
    /// </summary>
    public bool DisallowAll { get; set; }

    /// <summary>
    /// Пути приложения, закрытые от индексирования и на разрешённом хосте. Служебные пути самой
    /// веб-инфраструктуры (/health, /metrics) закрыты всегда, перечислять их не нужно.
    /// </summary>
    public List<string> DisallowedPaths { get; set; } = [];
}

public static class RobotsTxtExtensions
{
    /// <summary>
    /// Эндпойнты, которые заводит сама веб-инфраструктура: health-чеки и Prometheus-метрики.
    /// </summary>
    private static readonly string[] InfrastructurePaths = ["/health", "/metrics"];

    /// <summary>
    /// Отдаёт /robots.txt. Без него каждый заход поискового робота оставляет в логах 404.
    /// Вызывается из <see cref="WebInfrastructureRegistration.AddJoinWebPlatform"/>.
    /// </summary>
    /// <remarks>
    /// Ответ отдаёт middleware в самом начале конвейера (через <see cref="IStartupFilter"/>):
    /// приложению ничего не нужно маппить, а запрос не доходит ни до логирования,
    /// ни до аутентификации, ни до SPA-фоллбэков.
    /// </remarks>
    public static OptionsBuilder<RobotsTxtOptions> AddJoinRobotsTxt(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient<IStartupFilter, RobotsTxtStartupFilter>());
        return services.AddOptions<RobotsTxtOptions>().BindConfiguration(RobotsTxtOptions.SectionName);
    }

    internal static string BuildRobotsTxt(string requestHost, RobotsTxtOptions options)
    {
        var builder = new StringBuilder();
        _ = builder.Append("User-agent: *\n");

        var isMainHost = string.IsNullOrEmpty(options.MainHost) || IsSameHost(requestHost, options.MainHost);

        if (options.DisallowAll || !isMainHost)
        {
            _ = builder.Append("Disallow: /\n");
            return builder.ToString();
        }

        foreach (var path in InfrastructurePaths.Concat(options.DisallowedPaths))
        {
            _ = builder.Append("Disallow: ").Append(path).Append('\n');
        }
        return builder.ToString();
    }

    /// <summary>
    /// Если в <paramref name="mainHost"/> нет порта, порт из запроса не учитываем (<c>joinrpg.ru:443</c> — тот же хост).
    /// </summary>
    private static bool IsSameHost(string requestHost, string mainHost)
    {
        var expected = HostString.FromUriComponent(mainHost);
        var actual = HostString.FromUriComponent(requestHost);
        return string.Equals(actual.Host, expected.Host, StringComparison.OrdinalIgnoreCase)
            && (expected.Port is null || actual.Port == expected.Port);
    }

    private sealed class RobotsTxtStartupFilter(IOptions<RobotsTxtOptions> options) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            _ = app.Use(async (context, nextMiddleware) =>
            {
                var request = context.Request;
                if (request.Path.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase)
                    && (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)))
                {
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    await context.Response.WriteAsync(BuildRobotsTxt(request.Host.Value ?? "", options.Value), context.RequestAborted);
                    return;
                }
                await nextMiddleware(context);
            });
            next(app);
        };
    }
}
