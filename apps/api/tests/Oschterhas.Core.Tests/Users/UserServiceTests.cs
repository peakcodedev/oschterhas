using Oschterhas.Core.Users;
using Oschterhas.Data.Entities;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Tests.Users;

public class UserServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly UserService _service;

    public UserServiceTests() => _service = new UserService(_users);

    [Fact]
    public async Task OnlyPlatformAdminsMayListOrChangeUsers()
    {
        var ct = TestContext.Current.CancellationToken;

        Assert.Equal("auth.platform_admin_required", (await _service.ListAsync(TestData.Otto, ct)).Error!.Code);
        Assert.Equal("auth.platform_admin_required", (await _service.UpdateAsync(TestData.Otto, Guid.NewGuid(), new(false, null), ct)).Error!.Code);
    }

    [Fact]
    public async Task Admin_CanDisableAndPromoteOthers()
    {
        var user = new AppUser { Id = Guid.NewGuid(), IdentityIssuer = "i", IdentitySubject = "s", Email = "e", DisplayName = "d" };
        _users.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var dto = (await _service.UpdateAsync(TestData.Admin, user.Id, new(IsActive: false, IsPlatformAdmin: true), TestContext.Current.CancellationToken)).Value;

        Assert.False(dto.IsActive);
        Assert.True(dto.IsPlatformAdmin);
        Assert.True(dto.InvitationPending);
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(null, false)]
    public async Task Admin_CannotLockThemselvesOut(bool? isActive, bool? isPlatformAdmin)
    {
        var result = await _service.UpdateAsync(TestData.Admin, TestData.Admin.Id, new(isActive, isPlatformAdmin), TestContext.Current.CancellationToken);

        Assert.Equal("user.cannot_demote_self", result.Error!.Code);
    }
}
