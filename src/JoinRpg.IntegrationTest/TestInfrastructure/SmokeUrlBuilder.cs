using System.Text;
using JoinRpg.DomainTypes.Interfaces;
using Microsoft.AspNetCore.Routing.Patterns;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Собирает URL эндпоинта из шаблона маршрута и значений сида (#4956).
/// </summary>
/// <remarks>
/// Маршрут заполняется по именам параметров, а не по описанию каждой страницы: пока параметр
/// называется так же, как в сиде, новая страница попадает под смоук сама, без правок теста.
/// Что не заполнилось — не угадывается: эндпоинт честно объявляется непокрытым, и его нужно
/// либо досеять, либо записать в <see cref="SmokeExpectations"/> с причиной.
/// </remarks>
internal static class SmokeUrlBuilder
{
    private const string ProjectIdParameter = "projectId";

    public static SmokeUrl Build(SmokeEndpoint endpoint, SmokeParameterValues values)
    {
        var missing = new List<string>();
        var fromRoute = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var segments = new List<string>();

        foreach (var segment in endpoint.Pattern.PathSegments)
        {
            var text = new StringBuilder();
            var skipSegment = false;

            foreach (var part in segment.Parts)
            {
                switch (part)
                {
                    case RoutePatternLiteralPart literal:
                        _ = text.Append(literal.Content);
                        break;
                    case RoutePatternSeparatorPart separator:
                        _ = text.Append(separator.Content);
                        break;
                    case RoutePatternParameterPart parameter
                        when values.TryGetValue(parameter.Name, out var value):
                        _ = fromRoute.Add(parameter.Name);
                        _ = text.Append(value.RouteValue);
                        break;
                    case RoutePatternParameterPart parameter
                        when parameter.IsOptional || parameter.Default is not null:
                        // Необязательный сегмент просто не пишем — страница откроется со значением
                        // по умолчанию (например, корневой группой).
                        skipSegment = true;
                        break;
                    case RoutePatternParameterPart parameter:
                        missing.Add(parameter.Name);
                        break;
                }
            }

            if (!skipSegment)
            {
                segments.Add(text.ToString());
            }
        }

        if (missing.Count > 0)
        {
            return new SmokeUrl(null, missing);
        }

        var url = string.Join('/', segments);
        var query = BuildQuery(endpoint, values, fromRoute);

        return new SmokeUrl(query.Length > 0 ? $"{url}?{query}" : url, []);
    }

    /// <summary>
    /// Query-string из тех параметров экшена, которые не приехали маршрутом, а значение для них есть.
    /// </summary>
    /// <remarks>
    /// Чего в сиде нет — не пишем вовсе: необязательные параметры (<c>export</c>, <c>version</c>)
    /// страница и так переживает, а из-за обязательного она ответит не 200, и это как раз то,
    /// что смоук должен показать, а не замаскировать.
    /// </remarks>
    private static string BuildQuery(
        SmokeEndpoint endpoint,
        SmokeParameterValues values,
        HashSet<string> fromRoute)
    {
        var parts = new List<string>();
        var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var parameter in endpoint.Parameters)
        {
            if (fromRoute.Contains(parameter.Name) || !values.TryGetValue(parameter.Name, out var value))
            {
                continue;
            }

            _ = added.Add(parameter.Name);
            parts.Add($"{Uri.EscapeDataString(parameter.Name)}={Uri.EscapeDataString(value.ForQuery(parameter.Type))}");
        }

        if (NeedsExplicitProjectId(endpoint, fromRoute, added) && values.TryGetValue(ProjectIdParameter, out var projectId))
        {
            parts.Add($"{ProjectIdParameter}={Uri.EscapeDataString(projectId.RouteValue)}");
        }

        return string.Join('&', parts);
    }

    /// <summary>
    /// Надо ли дописать <c>projectId</c>, которого сам эндпоинт не объявил.
    /// </summary>
    /// <remarks>
    /// Проект определяется только по пути или по параметру <c>projectId</c>
    /// (<c>DiscoverProjectMiddleware</c>), поэтому эндпоинт, который принимает id сущности проекта,
    /// но не сам проект, без этого параметра не проходит проверку мастерского доступа и уезжает на
    /// страницу входа. Клиенты webapi в Blazor так и ходят: `?projectId=1&amp;characterGroupId=2`.
    /// Страницам вне проекта параметр не дописываем — он сбил бы им контекст.
    /// </remarks>
    private static bool NeedsExplicitProjectId(
        SmokeEndpoint endpoint,
        HashSet<string> fromRoute,
        HashSet<string> added)
        => !fromRoute.Contains(ProjectIdParameter)
            && !added.Contains(ProjectIdParameter)
            && endpoint.Parameters.Any(p => typeof(IProjectEntityId).IsAssignableFrom(p.Type));
}

/// <summary>
/// Результат сборки URL: либо адрес, либо список параметров, которых не нашлось в сиде.
/// </summary>
internal sealed record SmokeUrl(string? Url, IReadOnlyList<string> MissingParameters);
