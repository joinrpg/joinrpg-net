using System.Net;
using System.Net.Sockets;

namespace JoinRpg.IdPortal.OAuthServer.Cimd;

/// <summary>
/// Защита от SSRF при скачивании Client ID Metadata Document: сервер ходит по URL, который
/// прислал клиент, поэтому в приватную сеть его пускать нельзя
/// (<see href="https://drafts.oauth.net/draft-ietf-oauth-client-id-metadata-document/draft-ietf-oauth-client-id-metadata-document.html">CIMD</see>, §6.5).
/// </summary>
public static class CimdAddressGuard
{
    /// <summary>
    /// Реестры адресов специального назначения, <see href="https://www.rfc-editor.org/info/rfc6890">RFC 6890</see>:
    /// выписаны все записи, у которых в колонке «Globally Reachable» стоит False, плюс multicast.
    /// Готового предиката в .NET нет — есть только <see cref="IPAddress.IsLoopback"/> и
    /// несколько IPv6-свойств, которые покрывают лишь часть реестра. Зато
    /// <see cref="IPNetwork"/> позволяет записать сам реестр, а не выводить его из арифметики
    /// по октетам: так список сверяется с RFC глазами.
    /// </summary>
    /// <remarks>
    /// Разрешаем только глобально маршрутизируемые адреса, то есть список — запрещающий.
    /// Это безопаснее обратного: новый специальный диапазон, о котором мы не знаем, окажется
    /// разрешён, но своей сети мы им не откроем.
    /// </remarks>
    private static readonly IPNetwork[] NotGloballyReachable =
    [
        // IPv4
        IPNetwork.Parse("0.0.0.0/8"),          // «этот» хост в «этой» сети
        IPNetwork.Parse("10.0.0.0/8"),         // Private-Use
        IPNetwork.Parse("100.64.0.0/10"),      // Shared Address Space (CGNAT)
        IPNetwork.Parse("127.0.0.0/8"),        // Loopback
        IPNetwork.Parse("169.254.0.0/16"),     // Link Local — сюда же метадата облаков
        IPNetwork.Parse("172.16.0.0/12"),      // Private-Use
        IPNetwork.Parse("192.0.0.0/24"),       // IETF Protocol Assignments
        IPNetwork.Parse("192.0.2.0/24"),       // Documentation (TEST-NET-1)
        IPNetwork.Parse("192.88.99.0/24"),     // 6to4 Relay Anycast
        IPNetwork.Parse("192.168.0.0/16"),     // Private-Use
        IPNetwork.Parse("198.18.0.0/15"),      // Benchmarking
        IPNetwork.Parse("198.51.100.0/24"),    // Documentation (TEST-NET-2)
        IPNetwork.Parse("203.0.113.0/24"),     // Documentation (TEST-NET-3)
        IPNetwork.Parse("224.0.0.0/4"),        // Multicast
        IPNetwork.Parse("240.0.0.0/4"),        // Reserved, включая 255.255.255.255

        // IPv6
        IPNetwork.Parse("::/128"),             // Unspecified
        IPNetwork.Parse("::1/128"),            // Loopback
        IPNetwork.Parse("64:ff9b:1::/48"),     // Local-Use IPv4/IPv6 Translation
        IPNetwork.Parse("100::/64"),           // Discard-Only
        IPNetwork.Parse("2001::/23"),          // IETF Protocol Assignments, включая Teredo
        IPNetwork.Parse("2001:db8::/32"),      // Documentation
        IPNetwork.Parse("fc00::/7"),           // Unique-Local
        IPNetwork.Parse("fe80::/10"),          // Link-Local Unicast
        IPNetwork.Parse("ff00::/8"),           // Multicast
    ];

    /// <summary>
    /// Адрес, по которому ходить нельзя. IPv4, завёрнутый в IPv6 (<c>::ffff:10.0.0.1</c>),
    /// отдельной обработки не требует: <see cref="IPNetwork.Contains"/> сопоставляет его
    /// с IPv4-сетями сам.
    /// </summary>
    public static bool IsForbidden(IPAddress address) =>
        address.AddressFamily is not (AddressFamily.InterNetwork or AddressFamily.InterNetworkV6)
            // Незнакомое семейство адресов — безопаснее запретить.
            || Array.Exists(NotGloballyReachable, network => network.Contains(address));

    /// <summary>
    /// Резолвит хост и требует, чтобы ВСЕ полученные адреса были публичными. Проверять надо
    /// каждый: иначе домен с двумя A-записями (публичной и приватной) проскочит по одной из них.
    /// </summary>
    public static async Task<bool> IsHostAllowedAsync(string host, CancellationToken ct)
    {
        // Литерал IP в URL — резолвить нечего, проверяем как есть.
        if (IPAddress.TryParse(host, out var literal))
        {
            return !IsForbidden(literal);
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(host, ct);
        }
        catch (SocketException)
        {
            return false;
        }

        return addresses.Length > 0 && Array.TrueForAll(addresses, a => !IsForbidden(a));
    }
}
