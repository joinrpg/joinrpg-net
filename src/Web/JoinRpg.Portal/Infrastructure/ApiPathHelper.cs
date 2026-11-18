namespace JoinRpg.Portal.Infrastructure;

internal static class ApiPathHelper
{

    public static bool IsApiPathSegment(ReadOnlySpan<char> segment) => segment.SequenceEqual("x-api") || segment.SequenceEqual("x-game-api") || segment.SequenceEqual("webapi");

    public static bool IsExternalApiPath(this string path) => path.StartsWith("/x-api") || path.StartsWith("/x-game-api");

    /// <summary>Внешние API на JWT: x-api и x-game-api. Первый сегмент пути — ровно «x-api»/«x-game-api», не префикс.</summary>
    public static bool IsExternalApiPath(this PathString path)
    {
        if (!path.HasValue || path.Value.Length < 2 || path.Value[0] != '/')
        {
            return false;
        }
        var value = path.Value;
        var secondSlash = value.IndexOf('/', 1);
        var segment = secondSlash < 0 ? value.AsSpan(1) : value.AsSpan(1, secondSlash - 1);
        return segment.SequenceEqual("x-api") || segment.SequenceEqual("x-game-api");
    }

    /// <summary>Любое API (островов Blazor и внешнее), для которого отказы отдаём ProblemDetails, а не HTML-страницей.</summary>
    public static bool IsApiPath(this PathString path) => path.IsInternalApiPath() || path.IsExternalApiPath();

    /// <summary>API островов Blazor: в него ходят по cookie, а не по JWT.</summary>
    public static bool IsInternalApiPath(this PathString path) => path.StartsWithSegments("/webapi");
}
