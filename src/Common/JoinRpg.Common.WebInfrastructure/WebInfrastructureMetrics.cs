namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Метрики самой веб-инфраструктуры.
/// </summary>
public static class WebInfrastructureMetrics
{
    /// <summary>
    /// Имя Meter'а веб-инфраструктуры. Отдельное от имени приложения: <see cref="OpenTelemetryRegistration.AddJoinOpenTelemetry"/>
    /// экспортирует его в любом приложении, независимо от того, как называется meter самого приложения.
    /// </summary>
    public const string MeterName = "JoinRpg.WebInfrastructure";
}
