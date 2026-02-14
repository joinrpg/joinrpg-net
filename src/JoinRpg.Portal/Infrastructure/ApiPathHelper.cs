namespace JoinRpg.Portal.Infrastructure;

internal static class ApiPathHelper
{

    public static bool IsApiPathSegment(ReadOnlySpan<char> segment) => segment.SequenceEqual("x-api") || segment.SequenceEqual("x-game-api") || segment.SequenceEqual("webapi");

    public static bool IsExternalApiPath(this string path) => path.StartsWith("/x-api") || path.StartsWith("/x-game-api");
}
