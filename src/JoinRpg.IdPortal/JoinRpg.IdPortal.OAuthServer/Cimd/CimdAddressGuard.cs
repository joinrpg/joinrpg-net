using System.Net;
using System.Net.Sockets;

namespace JoinRpg.IdPortal.OAuthServer.Cimd;

/// <summary>
/// Защита от SSRF при скачивании Client ID Metadata Document: сервер ходит по URL, который
/// прислал клиент, поэтому в приватную сеть его пускать нельзя
/// (draft-ietf-oauth-client-id-metadata-document-00 §6.5).
/// </summary>
public static class CimdAddressGuard
{
    /// <summary>
    /// Адрес, по которому ходить нельзя: loopback, приватные диапазоны, link-local,
    /// multicast и прочее не-публичное — и в IPv4, и в IPv6.
    /// </summary>
    public static bool IsForbidden(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // IPv4, завёрнутый в IPv6 (::ffff:10.0.0.1), иначе обошёл бы проверки ниже.
            if (address.IsIPv4MappedToIPv6)
            {
                return IsForbidden(address.MapToIPv4());
            }

            return address.IsIPv6LinkLocal
                || address.IsIPv6SiteLocal
                || address.IsIPv6Multicast
                || address.IsIPv6UniqueLocal
                || address.Equals(IPAddress.IPv6Any);
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            // Незнакомое семейство адресов — безопаснее запретить.
            return true;
        }

        var octets = address.GetAddressBytes();
        return octets[0] switch
        {
            0 => true,                                     // 0.0.0.0/8
            10 => true,                                    // 10.0.0.0/8 приватный
            127 => true,                                   // loopback (подстраховка)
            169 when octets[1] == 254 => true,             // 169.254.0.0/16 link-local, сюда же метадата облаков
            172 when octets[1] is >= 16 and <= 31 => true, // 172.16.0.0/12 приватный
            192 when octets[1] == 168 => true,             // 192.168.0.0/16 приватный
            192 when octets[1] == 0 && octets[2] == 0 => true, // 192.0.0.0/24
            100 when octets[1] is >= 64 and <= 127 => true,   // 100.64.0.0/10 CGNAT
            >= 224 => true,                                // multicast и зарезервированные
            _ => false,
        };
    }

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
