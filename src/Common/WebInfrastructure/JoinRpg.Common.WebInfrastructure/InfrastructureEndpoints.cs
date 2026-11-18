namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Эндпойнты, которые заводит сама веб-инфраструктура: health-чеки и Prometheus-метрики.
/// На них ходят изнутри кластера напрямую на под (kubelet, Prometheus), мимо прокси.
/// </summary>
internal static class InfrastructureEndpoints
{
    public static readonly string[] PathPrefixes = ["/health", "/metrics"];

    public static bool IsInfrastructurePath(PathString path)
    {
        foreach (var prefix in PathPrefixes)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
