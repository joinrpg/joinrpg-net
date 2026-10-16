using System.Diagnostics.Metrics;
using System.Net;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Счётчик <c>joinrpg.http.requests.private_network</c> — запросы, у которых после обработки
/// X-Forwarded-* адрес клиента оказался внутренним (loopback, RFC1918, link-local, IPv6 ULA).
/// Нужен, чтобы заметить неправильную настройку ForwardedHeaders (см. <see cref="ForwardedHeadersExtensions"/>).
/// </summary>
/// <remarks>
/// <para>
/// Живые пользователи приходят с публичных адресов, поэтому в норме счётчик стоит на нуле.
/// Как читать по тегу <c>forwarded</c> (был ли в запросе X-Forwarded-For):
/// </para>
/// <list type="bullet">
/// <item><c>forwarded=false</c> — запрос пришёл в обход прокси: кто-то изнутри кластера
/// ходит напрямую на под (или прокси перестал ставить X-Forwarded-For).</item>
/// <item><c>forwarded=true</c> — прокси переслал внутренний адрес: вероятно, прокси больше одного
/// и <c>ForwardLimit = 1</c> не хватает — мы берём адрес соседнего прокси, а не клиента.</item>
/// </list>
/// <para>
/// Служебные запросы (/health*, /metrics) не считаются: kubelet и Prometheus ходят на под напрямую
/// с внутренних адресов, это нормально. Запросы без адреса клиента (TestServer) тоже не считаются.
/// </para>
/// </remarks>
internal sealed class PrivateNetworkRequestMetrics
{
    public const string CounterName = "joinrpg.http.requests.private_network";
    public const string ForwardedTagName = "forwarded";

    private static readonly IPNetwork[] PrivateNetworks =
    [
        // IPv4: loopback, RFC1918, link-local
        IPNetwork.Parse("127.0.0.0/8"),
        IPNetwork.Parse("10.0.0.0/8"),
        IPNetwork.Parse("172.16.0.0/12"),
        IPNetwork.Parse("192.168.0.0/16"),
        IPNetwork.Parse("169.254.0.0/16"),
        // IPv6: loopback, link-local, unique local (ULA)
        IPNetwork.Parse("::1/128"),
        IPNetwork.Parse("fe80::/10"),
        IPNetwork.Parse("fc00::/7"),
    ];

    private readonly Counter<long> counter;

    public PrivateNetworkRequestMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(WebInfrastructureMetrics.MeterName);
        counter = meter.CreateCounter<long>(
            CounterName,
            description: "Запросы с внутреннего адреса клиента после обработки X-Forwarded-*: признак неправильной настройки ForwardedHeaders");
    }

    /// <summary>
    /// Учесть запрос. Вызывается после ForwardedHeadersMiddleware.
    /// </summary>
    /// <param name="context">Запрос после ForwardedHeadersMiddleware.</param>
    /// <param name="hadForwardedFor">Был ли в запросе X-Forwarded-For до обработки.</param>
    public void Record(HttpContext context, bool hadForwardedFor)
    {
        if (InfrastructureEndpoints.IsInfrastructurePath(context.Request.Path))
        {
            return;
        }
        if (context.Connection.RemoteIpAddress is not { } address || !IsPrivateNetwork(address))
        {
            return;
        }
        counter.Add(1, new KeyValuePair<string, object?>(ForwardedTagName, hadForwardedFor ? "true" : "false"));
    }

    internal static bool IsPrivateNetwork(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }
        foreach (var network in PrivateNetworks)
        {
            if (network.Contains(address))
            {
                return true;
            }
        }
        return false;
    }
}
