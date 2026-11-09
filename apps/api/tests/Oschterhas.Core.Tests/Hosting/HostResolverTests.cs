using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Oschterhas.Core.Hosting;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Tests.Hosting;

public class HostResolverTests
{
    private static readonly Guid TannId = Guid.NewGuid();
    private static readonly Guid CeviId = Guid.NewGuid();

    private readonly IInstanceRepository _instances = Substitute.For<IInstanceRepository>();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly HostResolver _resolver;

    public HostResolverTests()
    {
        _instances.FindIdBySlugAsync("tann", Arg.Any<CancellationToken>()).Returns(TannId);
        _instances.FindIdByVerifiedCustomDomainAsync("ostern.ceviduernten.ch", Arg.Any<CancellationToken>()).Returns(CeviId);

        var options = Options.Create(new HostingOptions { BaseDomain = "oschterhas.li", AdminSubdomain = "admin" });
        _resolver = new HostResolver(options, _instances, _cache);
    }

    [Theory]
    [InlineData("oschterhas.li")]
    [InlineData("www.oschterhas.li")]
    [InlineData("OSCHTERHAS.LI.")]
    public async Task BaseDomain_IsLanding(string host)
    {
        Assert.Equal(HostResolution.Landing, await Resolve(host));
    }

    [Fact]
    public async Task AdminHost_IsAdmin()
    {
        Assert.Equal(HostResolution.Admin, await Resolve("admin.oschterhas.li"));
    }

    [Theory]
    [InlineData("tann.oschterhas.li")]
    [InlineData("Tann.Oschterhas.li")]
    [InlineData("tann.oschterhas.li.")]
    public async Task KnownSlug_IsInstance(string host)
    {
        Assert.Equal(HostResolution.ForInstance(TannId), await Resolve(host));
    }

    [Fact]
    public async Task VerifiedCustomDomain_IsInstance()
    {
        Assert.Equal(HostResolution.ForInstance(CeviId), await Resolve("ostern.ceviduernten.ch"));
    }

    [Theory]
    [InlineData("unbekannt.oschterhas.li")]
    [InlineData("andere.domain.ch")]
    public async Task UnknownHosts_AreUnknown(string host)
    {
        Assert.Equal(HostResolution.Unknown, await Resolve(host));
    }

    [Theory]
    [InlineData("a.b.oschterhas.li")]
    [InlineData("ab.oschterhas.li")]
    [InlineData("-tann.oschterhas.li")]
    [InlineData("custom.oschterhas.li")]
    [InlineData("127.0.0.1")]
    [InlineData("[::1]")]
    [InlineData("")]
    [InlineData("bad host")]
    public async Task InvalidOrReservedHosts_AreUnknown_WithoutDatabaseLookup(string host)
    {
        Assert.Equal(HostResolution.Unknown, await Resolve(host));

        await _instances.DidNotReceive().FindIdBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _instances.DidNotReceive().FindIdByVerifiedCustomDomainAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubdomainOfBaseDomain_IsNeverLookedUpAsCustomDomain()
    {
        await Resolve("unbekannt.oschterhas.li");

        await _instances.DidNotReceive().FindIdByVerifiedCustomDomainAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LookAlikeDomain_IsTreatedAsCustomDomain()
    {
        Assert.Equal(HostResolution.Unknown, await Resolve("tann.oschterhas.li.evil.ch"));

        await _instances.Received(1).FindIdByVerifiedCustomDomainAsync("tann.oschterhas.li.evil.ch", Arg.Any<CancellationToken>());
        await _instances.DidNotReceive().FindIdBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Results_AreCached()
    {
        await Resolve("tann.oschterhas.li");
        await Resolve("TANN.oschterhas.li");

        await _instances.Received(1).FindIdBySlugAsync("tann", Arg.Any<CancellationToken>());
    }

    private ValueTask<HostResolution> Resolve(string host) =>
        _resolver.ResolveAsync(host, TestContext.Current.CancellationToken);
}
