using System.Net;
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
}
