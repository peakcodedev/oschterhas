using System.Net;
using Oschterhas.Core.Domains;

namespace Oschterhas.Core.Tests.Domains;

public class AddressPolicyTests
{
    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("185.199.108.153")]
    [InlineData("2a00:1450:4001:82a::200e")]
    public void PublicAddresses_AreAllowed(string address)
    {
        Assert.True(AddressPolicy.IsPublic(IPAddress.Parse(address)));
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.1.2.3")]
    [InlineData("172.20.0.5")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.0.1")]
    [InlineData("0.0.0.0")]
    [InlineData("224.0.0.1")]
    [InlineData("::1")]
    [InlineData("::")]
    [InlineData("fd00::1")]
    [InlineData("fe80::1")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::ffff:10.0.0.1")]
    public void InternalAddresses_AreBlocked(string address)
    {
        Assert.False(AddressPolicy.IsPublic(IPAddress.Parse(address)));
    }
}
