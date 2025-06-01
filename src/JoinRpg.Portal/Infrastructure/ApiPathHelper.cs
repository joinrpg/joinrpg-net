namespace JoinRpg.Portal.Infrastructure;

internal static class ApiPathHelper
{
    public static bool IsApiPath(this string path)
    {
        var pathSpan = path.AsSpan();
        foreach (var segmentRange in pathSpan.Split('/'))
        {
            if (segmentRange.Start.Value == 0 && segmentRange.End.Value == 0) // пропускаем первый пустой
            {
                continue;
            }
            return IsApiPathSegment(pathSpan[segmentRange]);
        }
        return false;
    }

    public static bool IsApiPathSegment(ReadOnlySpan<char> segment) => segment.SequenceEqual("x-api") || segment.SequenceEqual("x-game-api") || segment.SequenceEqual("webapi");

    public static bool IsExternalApiPath(this string path) => path.StartsWith("/x-api") || path.StartsWith("/x-game-api");
}
