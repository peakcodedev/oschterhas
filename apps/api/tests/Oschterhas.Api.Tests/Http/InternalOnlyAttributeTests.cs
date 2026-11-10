using System.Net;
using Oschterhas.Api.Http;

namespace Oschterhas.Api.Tests.Http;

public class InternalOnlyAttributeTests
{
    private static readonly string[] Default = ["127.0.0.0/8", "::1/128"];

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("::ffff:127.0.0.1", true)]
    [InlineData("10.0.0.5", false)]
    [InlineData("8.8.8.8", false)]
    [InlineData("2a00:1450::1", false)]
    public void OnlyConfiguredNetworksAreAllowed(string address, bool allowed)
    {
        Assert.Equal(allowed, InternalOnlyAttribute.IsAllowed(IPAddress.Parse(address), Default));
    }

    [Fact]
    public void DockerNetworkCanBeAllowed()
    {
        Assert.True(InternalOnlyAttribute.IsAllowed(IPAddress.Parse("172.18.0.3"), ["172.16.0.0/12"]));
    }

    [Fact]
    public void UnknownRemoteAddress_IsRejected()
    {
        Assert.False(InternalOnlyAttribute.IsAllowed(null, Default));
    }

    [Fact]
    public void InvalidNetworkEntries_AreIgnored()
    {
        Assert.False(InternalOnlyAttribute.IsAllowed(IPAddress.Loopback, ["kein-netz"]));
    }
}
