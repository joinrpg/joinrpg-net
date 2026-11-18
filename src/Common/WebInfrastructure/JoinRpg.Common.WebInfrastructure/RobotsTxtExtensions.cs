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
    /// Отдаёт /robots.txt. Без него каждый заход поискового робота оставляет в логах 404.
    /// Вызывается из <c>WebInfrastructureRegistration.AddJoinWebPlatform</c> (всех перегрузок).
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

    /// <summary>
    /// Рендерящий бот Baidu (отдельный user-agent от обычного Baiduspider) прогоняет страницы
    /// через браузер и создаёт заметно большую нагрузку, чем обычный краулер.
    /// </summary>
    private const string BaiduspiderRenderUserAgent = "Baiduspider-render";

    /// <summary>
    /// Пауза между запросами для <see cref="BaiduspiderRenderUserAgent"/> (секунд).
    /// </summary>
    private const int BaiduspiderRenderCrawlDelaySeconds = 10;

    internal static string BuildRobotsTxt(string requestHost, RobotsTxtOptions options)
    {
        var isMainHost = string.IsNullOrEmpty(options.MainHost) || IsSameHost(requestHost, options.MainHost);

        if (options.DisallowAll || !isMainHost)
        {
            // См. комментарий ниже про порядок групп: «*» не соответствует Baiduspider-render (RFC 9309),
            // поэтому запрет ему дублируется явно.
            return new StringBuilder()
                .Append("User-agent: ").Append(BaiduspiderRenderUserAgent).Append('\n')
                .Append("Disallow: /\n")
                .Append('\n')
                .Append("User-agent: *\n")
                .Append("Disallow: /\n")
                .ToString();
        }

        var disallowedPaths = InfrastructureEndpoints.PathPrefixes.Concat(options.DisallowedPaths).ToList();

        // Специфичная группа идёт первой: у парсеров с семантикой «первая совпавшая группа»
        // (не RFC 9309) у Baiduspider-render не должны пропадать запреты из общей группы.
        var builder = new StringBuilder()
            .Append("User-agent: ").Append(BaiduspiderRenderUserAgent).Append('\n')
            .Append("Crawl-delay: ").Append(BaiduspiderRenderCrawlDelaySeconds).Append('\n');
        foreach (var path in disallowedPaths)
        {
            _ = builder.Append("Disallow: ").Append(path).Append('\n');
        }
        _ = builder.Append('\n');

        _ = builder.Append("User-agent: *\n");
        foreach (var path in disallowedPaths)
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
