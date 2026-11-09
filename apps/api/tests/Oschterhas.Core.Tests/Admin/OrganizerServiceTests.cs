using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Oschterhas.Common.Results;
using Oschterhas.Core.Admin;
using Oschterhas.Core.Identity;
using Oschterhas.Data.Entities;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Tests.Admin;

public class OrganizerServiceTests
{
    private readonly IInstanceRepository _instances = Substitute.For<IInstanceRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IIdentityAdminClient _identity = Substitute.For<IIdentityAdminClient>();
    private readonly OrganizerService _service;
    private readonly Instance _tann = TestData.Tann();

    public OrganizerServiceTests()
    {
        _instances.FindForUpdateAsync(_tann.Id, Arg.Any<CancellationToken>()).Returns(_tann);
        _identity.SendInvitationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        _service = new OrganizerService(
            _instances,
            _users,
            _identity,
            Options.Create(new IdentityAdminOptions { Issuer = "http://kc/realms/oschterhas" }),
            new FakeTimeProvider());
    }

    [Fact]
    public async Task NewPerson_IsCreatedAtIdentity_Invited_AndAssigned()
    {
        _identity.FindOrCreateUserAsync("neu@test.ch", "Nora Neu", Arg.Any<CancellationToken>())
            .Returns(Result<IdentityAccount>.Success(new IdentityAccount("kc-neu", Created: true)));

        var dto = (await Add(" Neu@Test.CH ", " Nora Neu ")).Value;

        Assert.True(dto.InvitationPending);
        Assert.Equal("neu@test.ch", dto.Email);
        _users.Received(1).Add(Arg.Is<AppUser>(u => u.IdentitySubject == "kc-neu" && u.IdentityIssuer == "http://kc/realms/oschterhas"));
        Assert.Equal(2, _tann.Organizers.Count);
        await _identity.Received(1).SendInvitationAsync("kc-neu", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistingAccount_IsAssignedWithoutNewInvitation()
    {
        var existing = new AppUser { Id = Guid.NewGuid(), IdentityIssuer = "http://kc/realms/oschterhas", IdentitySubject = "kc-olga", Email = "olga@test.ch", DisplayName = "Olga", LastLoginAt = DateTimeOffset.UnixEpoch };
        _identity.FindOrCreateUserAsync("olga@test.ch", "Olga", Arg.Any<CancellationToken>())
            .Returns(Result<IdentityAccount>.Success(new IdentityAccount("kc-olga", Created: false)));
        _users.FindByIdentityAsync("http://kc/realms/oschterhas", "kc-olga", Arg.Any<CancellationToken>()).Returns(existing);

        var dto = (await Add("olga@test.ch", "Olga")).Value;

        Assert.Equal(existing.Id, dto.UserId);
        Assert.False(dto.InvitationPending);
        _users.DidNotReceive().Add(Arg.Any<AppUser>());
        await _identity.DidNotReceive().SendInvitationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AlreadyAssigned_IsAConflict()
    {
        var otto = _tann.Organizers[0].User;
        _identity.FindOrCreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<IdentityAccount>.Success(new IdentityAccount(otto.IdentitySubject, Created: false)));
        _users.FindByIdentityAsync(Arg.Any<string>(), otto.IdentitySubject, Arg.Any<CancellationToken>()).Returns(otto);

        var result = await Add("otto@test", "Otto");

        Assert.Equal("organizer.already_assigned", result.Error!.Code);
    }

    [Theory]
    [InlineData("", "Name", "organizer.email_invalid")]
    [InlineData("kein-mail", "Name", "organizer.email_invalid")]
    [InlineData("Max <max@test.ch>", "Name", "organizer.email_invalid")]
    [InlineData("max@test.ch", " ", "organizer.name_invalid")]
    public async Task InvalidInput_IsRejectedBeforeCallingIdentity(string email, string name, string expectedCode)
    {
        var result = await Add(email, name);

        Assert.Equal(expectedCode, result.Error!.Code);
        await _identity.DidNotReceive().FindOrCreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnlyPlatformAdminsManageOrganizers()
    {
        var ct = TestContext.Current.CancellationToken;

        Assert.Equal("auth.platform_admin_required", (await _service.AddAsync(TestData.Otto, _tann.Id, new("a@b.ch", "A"), ct)).Error!.Code);
        Assert.Equal("auth.platform_admin_required", (await _service.RemoveAsync(TestData.Otto, _tann.Id, TestData.Otto.Id, ct)).Error!.Code);
        Assert.Equal("auth.platform_admin_required", (await _service.ResendInvitationAsync(TestData.Otto, _tann.Id, TestData.Otto.Id, ct)).Error!.Code);
    }

    [Fact]
    public async Task FailedInvitation_IsReported_ButOrganizerStaysAssigned()
    {
        _identity.FindOrCreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<IdentityAccount>.Success(new IdentityAccount("kc-neu", Created: true)));
        _identity.SendInvitationAsync("kc-neu", Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Unexpected("identity.unavailable", "down")));

        var result = await Add("neu@test.ch", "Neu");

        Assert.Equal("organizer.invitation_failed", result.Error!.Code);
        Assert.Equal(2, _tann.Organizers.Count);
        await _instances.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Remove_UnassignsOrganizer()
    {
        var result = await _service.RemoveAsync(TestData.Admin, _tann.Id, TestData.Otto.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Empty(_tann.Organizers);
    }

    [Fact]
    public async Task Resend_OnlyWhileInvitationIsPending()
    {
        var ct = TestContext.Current.CancellationToken;
        var otto = _tann.Organizers[0].User;

        Assert.Equal("organizer.already_active", (await _service.ResendInvitationAsync(TestData.Admin, _tann.Id, otto.Id, ct)).Error!.Code);

        otto.LastLoginAt = null;
        Assert.True((await _service.ResendInvitationAsync(TestData.Admin, _tann.Id, otto.Id, ct)).IsSuccess);
        await _identity.Received(1).SendInvitationAsync(otto.IdentitySubject, Arg.Any<CancellationToken>());
    }

    private Task<Result<OrganizerDto>> Add(string email, string name) =>
        _service.AddAsync(TestData.Admin, _tann.Id, new AddOrganizerRequest(email, name), TestContext.Current.CancellationToken);
}
