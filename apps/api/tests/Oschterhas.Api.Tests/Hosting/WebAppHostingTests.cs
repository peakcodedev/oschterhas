using Oschterhas.Api.Hosting;

namespace Oschterhas.Api.Tests.Hosting;

public class WebAppHostingTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/instanzen")]
    [InlineData("instanzen/3f2a/hasen")]
    [InlineData("/datenschutz")]
    [InlineData("/login")]
    [InlineData("/apis")]
    [InlineData(null)]
    public void AppRoutes_FallBackToIndex(string? path) =>
        Assert.True(WebAppHosting.IsSpaPath(path));

    [Theory]
    [InlineData("/api")]
    [InlineData("/api/public/instance")]
    [InlineData("api/unknown")]
    [InlineData("/API/x")]
    [InlineData("/internal/tls/allowed")]
    [InlineData("/health")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/.well-known/oschterhas-verification")]
    public void ServerPaths_DoNotFallBackToIndex(string path) =>
        Assert.False(WebAppHosting.IsSpaPath(path));

    [Theory]
    [InlineData("main-ABCD1234.js")]
    [InlineData("styles-Z9Y8X7W6.css")]
    [InlineData("poppins-latin-700-normal-TKEB4KWA.woff2")]
    [InlineData("layers-55W3Q4RM.png")]
    [InlineData("chunk-B2uA_3D3.js")]
    [InlineData("main-x7kq2pzd.js")]
    public void HashedFiles_AreCachedForAYear(string fileName) =>
        Assert.Equal("public, max-age=31536000, immutable", WebAppHosting.CacheControlFor(fileName));

    [Theory]
    [InlineData("index.html")]
    [InlineData("ngsw.json")]
    [InlineData("ngsw-worker.js")]
    [InlineData("manifest.webmanifest")]
    [InlineData("oschterhas-logo-quer.svg")]
    [InlineData("de.json")]
    [InlineData("icon-maskable-512.png")]
    [InlineData("oschterhas-negativ.svg")]
    public void UnhashedFiles_AreRevalidated(string fileName) =>
        Assert.Equal("no-cache", WebAppHosting.CacheControlFor(fileName));
}
