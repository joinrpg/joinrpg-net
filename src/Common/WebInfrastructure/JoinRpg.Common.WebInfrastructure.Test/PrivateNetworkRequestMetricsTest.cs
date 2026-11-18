using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure;

public class PrivateNetworkRequestMetricsTest
{
    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("127.10.20.30", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("10.255.255.255", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.31.255.255", true)]
    [InlineData("172.15.255.255", false)]
    [InlineData("172.32.0.1", false)]
    [InlineData("192.168.1.1", true)]
    [InlineData("192.169.0.1", false)]
    [InlineData("169.254.169.254", true)]
    [InlineData("8.8.8.8", false)]
    [InlineData("100.64.0.1", false)]
    [InlineData("::1", true)]
    [InlineData("fe80::1", true)]
    [InlineData("fe80::1%2", true)]
    [InlineData("fc00::1", true)]
    [InlineData("fd12:3456:789a::1", true)]
    [InlineData("2a00:1450:4010::1", false)]
    [InlineData("::ffff:10.1.2.3", true)]
    [InlineData("::ffff:127.0.0.1", true)]
    [InlineData("::ffff:8.8.8.8", false)]
    public void ClassifiesAddress(string address, bool expected)
        => PrivateNetworkRequestMetrics.IsPrivateNetwork(IPAddress.Parse(address)).ShouldBe(expected);

    [Theory]
    [InlineData("10.222.3.17", "10.222.3.0/24")]
    [InlineData("10.222.3.0", "10.222.3.0/24")]
    [InlineData("192.168.255.255", "192.168.255.0/24")]
    [InlineData("::ffff:10.1.2.3", "10.1.2.0/24")]
    [InlineData("127.0.0.1", "loopback")]
    [InlineData("127.10.20.30", "loopback")]
    [InlineData("::1", "loopback")]
    [InlineData("::ffff:127.0.0.1", "loopback")]
    [InlineData("fd12:3456:789a:1:2:3:4:5", "fd12:3456:789a:1::/64")]
    [InlineData("fe80::1%2", "fe80::/64")]
    public void GetsSubnet(string address, string expected)
        => PrivateNetworkRequestMetrics.GetSubnet(IPAddress.Parse(address)).ShouldBe(expected);

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(2, "2")]
    [InlineData(3, "3+")]
    [InlineData(10, "3+")]
    public void FormatsCount(int count, string expected)
        => PrivateNetworkRequestMetrics.FormatCount(count).ShouldBe(expected);

    [Theory]
    [InlineData(new string[0], 0)]
    [InlineData(new[] { "" }, 0)]
    [InlineData(new[] { "8.8.8.8" }, 1)]
    [InlineData(new[] { "8.8.8.8, 10.0.0.7" }, 2)]
    [InlineData(new[] { "8.8.8.8,10.0.0.7,10.0.0.8" }, 3)]
    [InlineData(new[] { "8.8.8.8", "10.0.0.7" }, 2)]
    [InlineData(new[] { "8.8.8.8, 10.0.0.7", "10.0.0.8" }, 3)]
    [InlineData(new[] { " , 8.8.8.8,, " }, 1)]
    public void CountsForwardedFor(string[] values, int expected)
        => IncomingForwardedHeaders.CountEntries(new StringValues(values)).ShouldBe(expected);

    [Theory]
    [InlineData(new string[0], "none")]
    [InlineData(new[] { "" }, "none")]
    [InlineData(new[] { "http" }, "http")]
    [InlineData(new[] { "https" }, "https")]
    [InlineData(new[] { "HTTPS" }, "https")]
    [InlineData(new[] { " https " }, "https")]
    [InlineData(new[] { "ws" }, "other")]
    // Применяется ближайшее к нам (последнее) значение
    [InlineData(new[] { "https, http" }, "http")]
    [InlineData(new[] { "http", "https" }, "https")]
    public void ClassifiesForwardedProto(string[] values, string expected)
        => IncomingForwardedHeaders.ClassifyProto(new StringValues(values)).ShouldBe(expected);

    [Theory]
    [InlineData("http", "http")]
    [InlineData("https", "https")]
    [InlineData("HTTPS", "https")]
    [InlineData("ws", "other")]
    [InlineData("", "other")]
    [InlineData(null, "other")]
    public void ClassifiesScheme(string? scheme, string expected)
        => PrivateNetworkRequestMetrics.ClassifyScheme(scheme).ShouldBe(expected);

    [Theory]
    [InlineData(null, "none")]
    [InlineData("", "none")]
    [InlineData("joinrpg.ru", "name")]
    [InlineData("joinrpg.ru:443", "name")]
    [InlineData("localhost", "name")]
    [InlineData("portal", "name")]
    [InlineData("10.222.3.17", "ip")]
    [InlineData("10.222.3.17:8080", "ip")]
    [InlineData("[::1]", "ip")]
    [InlineData("[fd12::1]:8080", "ip")]
    // Сокращённые формы IPv4 IPAddress.TryParse понимает, но в Host это имена
    [InlineData("10", "name")]
    [InlineData("10.1", "name")]
    [InlineData("1.2.3.example", "name")]
    public void ClassifiesHost(string? host, string expected)
        => PrivateNetworkRequestMetrics.GetHostKind(host is null ? default : new HostString(host)).ShouldBe(expected);
}
