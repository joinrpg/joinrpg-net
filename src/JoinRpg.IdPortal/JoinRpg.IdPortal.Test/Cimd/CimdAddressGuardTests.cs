using System.Net;
using JoinRpg.IdPortal.OAuthServer.Cimd;

namespace JoinRpg.IdPortal.Test.Cimd;

/// <summary>
/// Защита от SSRF (§6.5 драфта): сервер ходит по URL, который прислал клиент, поэтому в
/// приватную сеть его пускать нельзя. Случай с облачной метадатой (169.254.169.254) —
/// классический способ угнать учётки инстанса.
/// </summary>
public class CimdAddressGuardTests
{
    [Theory]
    [InlineData("127.0.0.1", "loopback")]
    [InlineData("127.1.2.3", "весь 127.0.0.0/8")]
    [InlineData("10.1.2.3", "приватный 10/8")]
    [InlineData("172.16.0.1", "приватный 172.16/12, нижняя граница")]
    [InlineData("172.31.255.255", "приватный 172.16/12, верхняя граница")]
    [InlineData("192.168.1.1", "приватный 192.168/16")]
    [InlineData("169.254.169.254", "метадата облака")]
    [InlineData("100.64.0.1", "CGNAT")]
    [InlineData("0.0.0.0", "0/8")]
    [InlineData("224.0.0.1", "multicast")]
    [InlineData("::1", "IPv6 loopback")]
    [InlineData("fe80::1", "IPv6 link-local")]
    [InlineData("fc00::1", "IPv6 unique local")]
    [InlineData("::ffff:10.0.0.1", "IPv4 в обёртке IPv6 — обошёл бы проверку по октетам")]
    // Записи RFC 6890, которые появились вместе с переходом на таблицу реестра
    [InlineData("192.0.2.1", "TEST-NET-1, документация")]
    [InlineData("198.51.100.1", "TEST-NET-2")]
    [InlineData("203.0.113.1", "TEST-NET-3")]
    [InlineData("198.18.0.1", "benchmarking")]
    [InlineData("192.88.99.1", "6to4 relay anycast")]
    [InlineData("240.0.0.1", "reserved")]
    [InlineData("255.255.255.255", "limited broadcast")]
    [InlineData("2001:db8::1", "IPv6 documentation")]
    [InlineData("::", "IPv6 unspecified")]
    public void Forbids(string address, string why) =>
        CimdAddressGuard.IsForbidden(IPAddress.Parse(address)).ShouldBeTrue(why);

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("172.15.0.1")] // рядом с приватным диапазоном, но вне его
    [InlineData("172.32.0.1")]
    [InlineData("2606:4700:4700::1111")]
    public void Allows(string address) =>
        CimdAddressGuard.IsForbidden(IPAddress.Parse(address)).ShouldBeFalse();

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.1")]
    [InlineData("[::1]")]
    public async Task IsHostAllowed_RejectsIpLiterals(string host)
    {
        // Литерал IP в URL резолвить нечего — проверяем как есть.
        var allowed = await CimdAddressGuard.IsHostAllowedAsync(host.Trim('[', ']'), CancellationToken.None);

        allowed.ShouldBeFalse();
    }

    [Fact]
    public async Task IsHostAllowed_RejectsUnresolvableHost()
    {
        var allowed = await CimdAddressGuard.IsHostAllowedAsync(
            $"nonexistent-{Guid.NewGuid():N}.invalid", CancellationToken.None);

        allowed.ShouldBeFalse();
    }
}
