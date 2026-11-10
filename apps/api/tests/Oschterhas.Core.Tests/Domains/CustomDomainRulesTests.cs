using Oschterhas.Core.Domains;

namespace Oschterhas.Core.Tests.Domains;

public class CustomDomainRulesTests
{
    [Theory]
    [InlineData("ostern.ceviduernten.ch", "ostern.ceviduernten.ch")]
    [InlineData("  Ostern.CeviDuernten.CH. ", "ostern.ceviduernten.ch")]
    [InlineData("https://ostern.ceviduernten.ch/", "ostern.ceviduernten.ch")]
    [InlineData("http://ostern.ceviduernten.ch:8080/pfad", "ostern.ceviduernten.ch")]
    [InlineData("ceviduernten.ch", "ceviduernten.ch")]
    [InlineData("ostern.dürnten.ch", "ostern.xn--drnten-3ya.ch")]
    public void AcceptsAndNormalizesDomains(string input, string expected)
    {
        var result = CustomDomainRules.Normalize(input, "oschterhas.li", allowLocalhost: false);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData(null, "customdomain.required")]
    [InlineData("  ", "customdomain.required")]
    [InlineData("ostern", "customdomain.invalid")]
    [InlineData("192.168.1.10", "customdomain.invalid")]
    [InlineData("[::1]", "customdomain.invalid")]
    [InlineData("ostern.ceviduernten.ch:8080", "customdomain.invalid")]
    [InlineData("ostern_cevi.ch", "customdomain.invalid")]
    [InlineData("-ostern.ch", "customdomain.invalid")]
    [InlineData("ostern.localhost", "customdomain.invalid")]
    [InlineData("localhost", "customdomain.invalid")]
    [InlineData("oschterhas.li", "customdomain.platform_domain")]
    [InlineData("tann.oschterhas.li", "customdomain.platform_domain")]
    [InlineData("ADMIN.Oschterhas.LI", "customdomain.platform_domain")]
    public void RejectsInvalidDomains(string? input, string expectedCode)
    {
        var result = CustomDomainRules.Normalize(input, "oschterhas.li", allowLocalhost: false);

        Assert.Equal(expectedCode, result.Error!.Code);
    }

    [Fact]
    public void RejectsTooLongDomains()
    {
        var label = new string('a', 60);
        var domain = string.Join('.', Enumerable.Repeat(label, 5)) + ".ch";

        Assert.Equal("customdomain.invalid", CustomDomainRules.Normalize(domain, "oschterhas.li", false).Error!.Code);
    }

    [Fact]
    public void LocalhostDomainsOnlyWhenAllowed()
    {
        var result = CustomDomainRules.Normalize("ostern.cevi-test.localhost", "oschterhas.localhost", allowLocalhost: true);

        Assert.Equal("ostern.cevi-test.localhost", result.Value);
    }

    [Fact]
    public void PlatformSubdomainsStayForbidden_EvenWithLocalhostAllowed()
    {
        var result = CustomDomainRules.Normalize("tann.oschterhas.localhost", "oschterhas.localhost", allowLocalhost: true);

        Assert.Equal("customdomain.platform_domain", result.Error!.Code);
    }
}
