using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing.Patterns;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Один GET-эндпоинт приложения, по которому ходит смоук по всем страницам (#4956).
/// </summary>
/// <param name="RouteTemplate">
/// Шаблон маршрута без ведущего слеша (<c>{projectId}/plots/Edit</c>) — им же эндпоинт
/// адресуется в <see cref="SmokeExpectations"/> и им же ключуется снапшот ленивых загрузок.
/// </param>
/// <param name="Action">Экшен или страница, к которой ведёт маршрут — только для сообщений об ошибках.</param>
/// <param name="Pattern">Разобранный шаблон: из него смоук собирает URL, подставляя значения сида.</param>
/// <param name="Parameters">
/// Параметры, которые эндпоинт принимает из URL — и из маршрута, и из query-string.
/// Тип нужен, чтобы понять, в каком виде подставлять значение: <c>int</c> или типизированный id.
/// </param>
internal sealed record SmokeEndpoint(
    string RouteTemplate,
    string Action,
    RoutePattern Pattern,
    IReadOnlyList<SmokeEndpointParameter> Parameters)
{
    public override string ToString() => $"GET /{RouteTemplate} ({Action})";
}

/// <summary>Параметр эндпоинта, который смоук подставляет в URL.</summary>
internal sealed record SmokeEndpointParameter(string Name, Type Type);

internal static class SmokeEndpointDiscovery
{
    /// <summary>
    /// Все GET-эндпоинты приложения, до которых смоуку есть смысл ходить.
    /// </summary>
    /// <remarks>
    /// Берём <see cref="EndpointDataSource"/> уже поднятого хоста, а не рефлексию по контроллерам:
    /// так в список попадают и Razor Pages, и маршруты, навешанные вторым атрибутом, — то есть
    /// ровно то, что реально отвечает на запросы.
    ///
    /// Что отсеивается тут же, без записи в <see cref="SmokeExpectations"/>:
    /// <list type="bullet">
    /// <item>эндпоинты без <see cref="ActionDescriptor"/> — статика, health-чеки, точки Blazor;</item>
    /// <item>конвенциональные маршруты (<c>{controller}/{action}</c>) — это страницы аккаунта и
    /// профиля, а не страницы проекта. У них у всех один и тот же шаблон маршрута, поэтому в
    /// снапшоте ленивых загрузок они слились бы в одно ведро с максимумом по всем экшенам —
    /// проверка превратилась бы в фикцию. Их обход требует нормализации ключа снапшота
    /// до пары «контроллер+экшен», это отдельная задача.</item>
    /// </list>
    /// </remarks>
    public static IReadOnlyList<SmokeEndpoint> DiscoverGetEndpoints(IServiceProvider services)
    {
        var endpoints = services.GetRequiredService<EndpointDataSource>().Endpoints;
        var result = new List<SmokeEndpoint>();

        foreach (var endpoint in endpoints.OfType<RouteEndpoint>())
        {
            var action = endpoint.Metadata.GetMetadata<ActionDescriptor>();
            if (action is null)
            {
                continue;
            }

            // Razor Pages метаданных о методах не объявляют — они разбирают верб обработчиком,
            // и GET у них есть всегда. Отсеиваем только то, что про GET сказало «нет».
            var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
            if (methods is { Count: > 0 } && !methods.Contains(HttpMethods.Get))
            {
                continue;
            }

            var template = (endpoint.RoutePattern.RawText ?? "").TrimStart('/');
            if (template.Contains("{controller", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(new SmokeEndpoint(
                template,
                DescribeAction(action),
                endpoint.RoutePattern,
                GetUrlParameters(action)));
        }

        return [.. result
            .DistinctBy(e => (e.RouteTemplate, e.Action))
            .OrderBy(e => e.RouteTemplate, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.Action, StringComparer.OrdinalIgnoreCase)];
    }

    private static string DescribeAction(ActionDescriptor action) => action switch
    {
        ControllerActionDescriptor controller => $"{controller.ControllerName}.{controller.ActionName}",
        PageActionDescriptor page => page.ViewEnginePath,
        _ => action.DisplayName ?? "?",
    };

    /// <summary>
    /// Параметры, которые приезжают из URL. Всё, что биндится из тела, форм, заголовков и DI,
    /// смоуку подставлять не надо — и нечем.
    /// </summary>
    private static IReadOnlyList<SmokeEndpointParameter> GetUrlParameters(ActionDescriptor action)
    {
        // У Razor Pages параметры экшена не описаны: значения приезжают в свойства страницы.
        var descriptors = action is CompiledPageActionDescriptor page
            ? page.BoundProperties
            : action.Parameters;

        return [.. descriptors
            .Where(p => IsFromUrl(p.BindingInfo?.BindingSource))
            .Select(p => new SmokeEndpointParameter(p.Name, p.ParameterType))];
    }

    private static bool IsFromUrl(BindingSource? source)
        // null — биндер сам разберётся, откуда брать (обычно маршрут или query).
        => source is null
            || source == BindingSource.Query
            || source == BindingSource.Path
            || source == BindingSource.ModelBinding
            || source == BindingSource.Custom;
}
