using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Oschterhas.Api.Auth;
using Oschterhas.Api.Http;

namespace Oschterhas.Api.Tests.Http;

public class SecurityEventLoggingTests
{
    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(429)]
    public async Task RejectedApiRequests_AreLoggedWithoutIp(int status)
    {
        var logger = new ListLogger<SecurityEventLogging>();
        var context = Context("/api/instances", status);
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.7");

        await new SecurityEventLogging(_ => Task.CompletedTask, logger).InvokeAsync(context);

        var (level, message) = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, level);
        Assert.Contains($"POST /api/instances answered {status} (user anonymous)", message, StringComparison.Ordinal);
        Assert.DoesNotContain("203.0.113.7", message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SignedInUser_IsNamed()
    {
        var logger = new ListLogger<SecurityEventLogging>();
        var context = Context("/api/users", 403);
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(AuthenticationSetup.UserIdClaim, "u-42")], "Cookies"));

        await new SecurityEventLogging(_ => Task.CompletedTask, logger).InvokeAsync(context);

        Assert.Contains("(user u-42)", Assert.Single(logger.Entries).Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/api/instances", 200)]
    [InlineData("/api/instances", 404)]
    [InlineData("/health", 401)]
    public async Task OtherResponses_AreNotLogged(string path, int status)
    {
        var logger = new ListLogger<SecurityEventLogging>();

        await new SecurityEventLogging(_ => Task.CompletedTask, logger).InvokeAsync(Context(path, status));

        Assert.Empty(logger.Entries);
    }

    private static DefaultHttpContext Context(string path, int status)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = path;
        context.Response.StatusCode = status;
        return context;
    }
}
