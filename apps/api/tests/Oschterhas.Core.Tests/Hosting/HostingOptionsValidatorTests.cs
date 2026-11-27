using Oschterhas.Core.Hosting;

namespace Oschterhas.Core.Tests.Hosting;

public class HostingOptionsValidatorTests
{
    private readonly HostingOptionsValidator _validator = new();

    [Theory]
    [InlineData("oschterhas.li", "admin")]
    [InlineData("oschterhas.localhost", "admin")]
    [InlineData("staging.oschterhas.li", "verwaltung")]
    public void AcceptsValidOptions(string baseDomain, string adminSubdomain)
    {
        var result = _validator.Validate(null, new HostingOptions { BaseDomain = baseDomain, AdminSubdomain = adminSubdomain });

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("localhost")]
    [InlineData("Oschterhas.li")]
    [InlineData("oschterhas.li/")]
    [InlineData("https://oschterhas.li")]
    [InlineData("127.0.0.1")]
    public void RejectsInvalidBaseDomain(string baseDomain)
    {
        var result = _validator.Validate(null, new HostingOptions { BaseDomain = baseDomain });

        Assert.True(result.Failed);
        Assert.Contains("BaseDomain", result.FailureMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Admin")]
    [InlineData("admin.panel")]
    public void RejectsInvalidAdminSubdomain(string adminSubdomain)
    {
        var result = _validator.Validate(null, new HostingOptions { BaseDomain = "oschterhas.li", AdminSubdomain = adminSubdomain });

        Assert.True(result.Failed);
        Assert.Contains("AdminSubdomain", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminHost_CombinesSubdomainAndBaseDomain()
    {
        var options = new HostingOptions { BaseDomain = "oschterhas.li", AdminSubdomain = "admin" };

        Assert.Equal("admin.oschterhas.li", options.AdminHost);
    }
}
