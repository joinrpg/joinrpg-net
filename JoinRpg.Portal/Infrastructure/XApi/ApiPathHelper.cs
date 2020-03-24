namespace JoinRpg.Portal.Infrastructure
{
    internal static class ApiPathHelper
    {
        public static bool IsApiPath(this string path) => path.Contains("x-api") || path.Contains("x-game-api");
    }
}
