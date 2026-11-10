using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Oschterhas.Core.Users;
using Oschterhas.Data.Entities;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Tests.Users;

public class LoginServiceTests
{
    private static readonly DateTimeOffset Now = new(2027, 3, 1, 8, 0, 0, TimeSpan.Zero);
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly AuthOptions _options = new();
    private readonly LoginService _service;

    public LoginServiceTests() =>
        _service = new LoginService(_users, Options.Create(_options), new FakeTimeProvider(Now), NullLogger<LoginService>.Instance);

    [Fact]
    public async Task KnownActiveUser_SignsIn_AndDisplayDataIsUpdated()
    {
        var user = new AppUser { Id = Guid.NewGuid(), IdentityIssuer = "iss", IdentitySubject = "sub", Email = "old@test", DisplayName = "Old" };
        _users.FindByIdentityAsync("iss", "sub", Arg.Any<CancellationToken>()).Returns(user);

        var result = await SignIn(new ExternalIdentity("iss", "sub", "new@test", "New Name"));

        Assert.Equal(new CurrentUser(user.Id, "new@test", "New Name", false), result.Value);
        Assert.Equal(Now, user.LastLoginAt);
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnknownUser_HasNoAccess()
    {
        var result = await SignIn(new ExternalIdentity("iss", "nobody", "nina@test", "Nina"));

        Assert.Equal("auth.no_access", result.Error!.Code);
        _users.DidNotReceive().Add(Arg.Any<AppUser>());
    }

    [Fact]
    public async Task SameSubjectFromAnotherIssuer_HasNoAccess()
    {
        _users.FindByIdentityAsync("iss", "sub", Arg.Any<CancellationToken>()).Returns(new AppUser
        {
            IdentityIssuer = "iss",
            IdentitySubject = "sub",
            Email = "e",
            DisplayName = "d",
        });

        var result = await SignIn(new ExternalIdentity("other-issuer", "sub", null, null));

        Assert.Equal("auth.no_access", result.Error!.Code);
    }

    [Fact]
    public async Task DisabledUser_IsRejected()
    {
        _users.FindByIdentityAsync("iss", "sub", Arg.Any<CancellationToken>()).Returns(new AppUser
        {
            IdentityIssuer = "iss",
            IdentitySubject = "sub",
            Email = "e",
            DisplayName = "d",
            IsActive = false,
        });

        var result = await SignIn(new ExternalIdentity("iss", "sub", null, null));

        Assert.Equal("auth.disabled", result.Error!.Code);
    }

    [Fact]
    public async Task InitialPlatformAdmin_IsCreatedOnFirstLogin()
    {
        _options.InitialPlatformAdmins.Add("boot-sub");

        var result = await SignIn(new ExternalIdentity("iss", "boot-sub", "boot@test", "Boot"));

        Assert.True(result.Value.IsPlatformAdmin);
        _users.Received(1).Add(Arg.Is<AppUser>(u => u.IdentitySubject == "boot-sub" && u.IsPlatformAdmin && u.Email == "boot@test"));
    }

    [Fact]
    public async Task GetActiveUser_ReturnsNullForDisabledOrMissingUsers()
    {
        var active = new AppUser { Id = Guid.NewGuid(), IdentityIssuer = "i", IdentitySubject = "s", Email = "e", DisplayName = "d" };
        var disabled = new AppUser { Id = Guid.NewGuid(), IdentityIssuer = "i", IdentitySubject = "t", Email = "e", DisplayName = "d", IsActive = false };
        _users.FindByIdAsync(active.Id, Arg.Any<CancellationToken>()).Returns(active);
        _users.FindByIdAsync(disabled.Id, Arg.Any<CancellationToken>()).Returns(disabled);
        var ct = TestContext.Current.CancellationToken;

        Assert.NotNull(await _service.GetActiveUserAsync(active.Id, ct));
        Assert.Null(await _service.GetActiveUserAsync(disabled.Id, ct));
        Assert.Null(await _service.GetActiveUserAsync(Guid.NewGuid(), ct));
    }

    private Task<Common.Results.Result<CurrentUser>> SignIn(ExternalIdentity identity) =>
        _service.SignInAsync(identity, TestContext.Current.CancellationToken);
}
