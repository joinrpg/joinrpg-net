using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Sockets;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Счётчик <c>joinrpg.http.requests.private_network</c> — запросы, у которых после обработки
/// X-Forwarded-* адрес клиента оказался внутренним (loopback, RFC1918, link-local, IPv6 ULA).
/// Нужен, чтобы заметить неправильную настройку ForwardedHeaders (см. <see cref="ForwardedHeadersExtensions"/>).
/// </summary>
/// <remarks>
/// <para>
/// Живые пользователи приходят с публичных адресов, поэтому в норме счётчик стоит на нуле.
/// Логов на проде может не быть, поэтому теги подобраны так, чтобы по одним метрикам понять,
/// откуда идут такие запросы. Кардинальность у всех тегов ограничена: полных адресов и строк
/// из запроса в тегах нет.
/// </para>
/// <list type="bullet">
/// <item><c>forwarded</c> — <c>true</c>/<c>false</c>: был ли в запросе X-Forwarded-For
/// (то же, что <c>xff.count != 0</c>; оставлен ради существующих графиков).</item>
/// <item><c>client.subnet</c> — подсеть итогового адреса клиента (после ForwardedHeaders):
/// для IPv4 — /24 (<c>10.222.3.0/24</c>), для IPv6 — /64 (<c>fd00:1:2:3::/64</c>), loopback — <c>loopback</c>.
/// Сверять с pod CIDR, node CIDR и адресами балансировщика кластера.</item>
/// <item><c>xff.count</c> — сколько адресов было в X-Forwarded-For до обработки:
/// <c>0</c>, <c>1</c>, <c>2</c>, <c>3+</c>.</item>
/// <item><c>xfp</c> — X-Forwarded-Proto до обработки (ближайшее к нам значение, его и применяет
/// <c>ForwardLimit = 1</c>): <c>none</c>, <c>http</c>, <c>https</c>, <c>other</c>.</item>
/// <item><c>host.kind</c> — Host запроса: <c>ip</c> (IP-литерал, с портом или без), <c>name</c>
/// (доменное имя), <c>none</c> (Host пустой).</item>
/// <item><c>scheme</c> — итоговый <c>Request.Scheme</c> после ForwardedHeaders: <c>http</c>, <c>https</c>,
/// <c>other</c>.</item>
/// </list>
/// <para>Как читать сочетания:</para>
/// <list type="bullet">
/// <item><c>forwarded=false</c> (<c>xff.count=0</c>), <c>host.kind=ip</c> — прямые обращения на под
/// по IP в обход Gateway (кто-то внутри кластера, сканер в pod-сети и т.п.).
/// <c>client.subnet</c> покажет, откуда: из pod CIDR — соседний под, из node CIDR — нода.</item>
/// <item><c>xff.count=0</c>, <c>host.kind=name</c> — пришло по имени, но прокси не поставил
/// X-Forwarded-For: либо прокси перестал его ставить, либо кто-то изнутри ходит на сервис по имени.</item>
/// <item><c>forwarded=true</c>, <c>xff.count=1</c>, <c>client.subnet</c> из pod CIDR или node CIDR —
/// прокси сам стоит во внутренней сети и пересылает в X-Forwarded-For адрес соседнего прокси или
/// балансировщика, а не клиента. Чинить на стороне прокси (например, сохранение адреса клиента
/// на балансировщике, PROXY protocol) — наш ForwardLimit тут ни при чём.</item>
/// <item><c>xff.count=2</c> или <c>3+</c> — перед подом цепочка прокси, и <c>ForwardLimit = 1</c>
/// берёт адрес ближайшего из них. Поднять ForwardLimit или задать KnownIPNetworks
/// (<c>client.subnet</c> подскажет, какие сети доверенные).</item>
/// <item><c>xfp=none</c> при <c>forwarded=true</c> — прокси ставит X-Forwarded-For, но не X-Forwarded-Proto:
/// схема запроса останется http, и редирект на https зациклится.</item>
/// <item><c>scheme=http</c> при <c>xfp=https</c> — X-Forwarded-Proto пришёл, но middleware его не применил
/// (например, число элементов в X-Forwarded-For и X-Forwarded-Proto не совпало при включённом
/// <c>RequireHeaderSymmetry</c>; по умолчанию он выключен, и несовпадение схему не ломает).
/// Перед включением редиректа на https (#5588) у запросов через прокси должно быть <c>scheme=https</c>,
/// иначе редирект зациклится.</item>
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
    public const string ClientSubnetTagName = "client.subnet";
    public const string XffCountTagName = "xff.count";
    public const string XfpTagName = "xfp";
    public const string HostKindTagName = "host.kind";
    public const string SchemeTagName = "scheme";

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
    /// <param name="incoming">Заголовки X-Forwarded-*, какими они были до обработки (<see cref="IncomingForwardedHeaders.Capture"/>).</param>
    public void Record(HttpContext context, IncomingForwardedHeaders incoming)
    {
        if (InfrastructureEndpoints.IsInfrastructurePath(context.Request.Path))
        {
            return;
        }
        if (context.Connection.RemoteIpAddress is not { } address || !IsPrivateNetwork(address))
        {
            return;
        }
        var tags = new TagList
        {
            { ForwardedTagName, incoming.ForwardedForCount > 0 ? "true" : "false" },
            { ClientSubnetTagName, GetSubnet(address) },
            { XffCountTagName, FormatCount(incoming.ForwardedForCount) },
            { XfpTagName, incoming.ForwardedProto },
            { HostKindTagName, GetHostKind(context.Request.Host) },
            { SchemeTagName, ClassifyScheme(context.Request.Scheme) },
        };
        counter.Add(1, tags);
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

    /// <summary>
    /// Подсеть адреса для тега: IPv4 — /24, IPv6 — /64, loopback — <c>loopback</c>.
    /// </summary>
    internal static string GetSubnet(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }
        if (IPAddress.IsLoopback(address))
        {
            return "loopback";
        }
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.0/24";
        }
        // Обнуляем идентификатор интерфейса; scope id (у link-local) при этом тоже отбрасывается
        Array.Clear(bytes, 8, bytes.Length - 8);
        return $"{new IPAddress(bytes)}/64";
    }

    /// <summary>
    /// Схема запроса для тега: <c>http</c>, <c>https</c> или <c>other</c>.
    /// </summary>
    internal static string ClassifyScheme(string? scheme) => scheme switch
    {
        _ when string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase) => "https",
        _ when string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase) => "http",
        _ => "other",
    };

    internal static string FormatCount(int count) => count switch
    {
        <= 0 => "0",
        1 => "1",
        2 => "2",
        _ => "3+",
    };

    /// <summary>
    /// Вид Host запроса: <c>ip</c> — IP-литерал (с портом или без, IPv6 в скобках), <c>name</c> — имя, <c>none</c> — пусто.
    /// </summary>
    internal static string GetHostKind(HostString host)
    {
        if (!host.HasValue || string.IsNullOrWhiteSpace(host.Host))
        {
            return "none";
        }
        // HostString.Host отрезает порт, у IPv6 скобки остаются
        var name = host.Host;
        if (name.StartsWith('[') && name.EndsWith(']'))
        {
            return IPAddress.TryParse(name.AsSpan(1, name.Length - 2), out _) ? "ip" : "name";
        }
        // IPAddress.TryParse понимает и сокращённые формы вроде "10" или "10.1",
        // поэтому IPv4 признаём только в полной записи из четырёх чисел
        if (name.Count(c => c == '.') == 3
            && IPAddress.TryParse(name, out var ipv4)
            && ipv4.AddressFamily == AddressFamily.InterNetwork)
        {
            return "ip";
        }
        return "name";
    }
}
