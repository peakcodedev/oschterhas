using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Oschterhas.Core.Domains;
using Oschterhas.Core.Hosting;
using Oschterhas.Data.Entities;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Tests.Domains;

public class CustomDomainServiceTests
{
    private readonly IInstanceRepository _instances = Substitute.For<IInstanceRepository>();
    private readonly IDomainVerifier _verifier = Substitute.For<IDomainVerifier>();
    private readonly IHostResolver _hostResolver = Substitute.For<IHostResolver>();
    private readonly IHostCacheInvalidator _hostCache = Substitute.For<IHostCacheInvalidator>();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2027, 3, 1, 8, 0, 0, TimeSpan.Zero));
    private readonly CustomDomainService _service;
    private readonly Instance _tann = TestData.Tann();

    public CustomDomainServiceTests()
    {
        _instances.FindForUpdateAsync(_tann.Id, Arg.Any<CancellationToken>()).Returns(_tann);
        _service = new CustomDomainService(
            _instances,
            _verifier,
            _hostResolver,
            _hostCache,
            Options.Create(new CustomDomainOptions()),
            Options.Create(new HostingOptions { BaseDomain = "oschterhas.li", PublicScheme = "https" }),
            _time);
    }

    [Fact]
    public async Task Set_OnlyByPlatformAdmin()
    {
        var result = await _service.SetAsync(TestData.Otto, _tann.Id, new("ostern.ceviduernten.ch"), TestContext.Current.CancellationToken);

        Assert.Equal("auth.platform_admin_required", result.Error!.Code);
        Assert.Null(_tann.CustomDomain);
    }

    [Fact]
    public async Task Set_StoresPendingDomainWithFreshToken_AndInvalidatesHostCache()
    {
        var dto = (await Set("https://Ostern.CeviDuernten.ch/")).Value;

        Assert.Equal("ostern.ceviduernten.ch", dto.CustomDomain);
        Assert.Equal(CustomDomainStatus.Pending, dto.CustomDomainStatus);
        Assert.Equal("https://tann.oschterhas.li", dto.PublicUrl);
        Assert.Equal(43, _tann.CustomDomainToken!.Length);
        _hostCache.Received(1).Invalidate();
    }

    [Fact]
    public async Task Set_NewDomain_ReplacesTokenAndResetsVerification()
    {
        await Set("ostern.ceviduernten.ch");
        var firstToken = _tann.CustomDomainToken;
        _tann.CustomDomainStatus = CustomDomainStatus.Verified;

        await Set("hasen.ceviduernten.ch");

        Assert.NotEqual(firstToken, _tann.CustomDomainToken);
        Assert.Equal(CustomDomainStatus.Pending, _tann.CustomDomainStatus);
        Assert.Null(_tann.CustomDomainVerifiedAt);
    }

    [Fact]
    public async Task Set_SameDomainAgain_ChangesNothing()
    {
        await Set("ostern.ceviduernten.ch");
        _tann.CustomDomainStatus = CustomDomainStatus.Verified;
        var token = _tann.CustomDomainToken;

        await Set("ostern.ceviduernten.ch");

        Assert.Equal(CustomDomainStatus.Verified, _tann.CustomDomainStatus);
        Assert.Equal(token, _tann.CustomDomainToken);
    }

    [Fact]
    public async Task Set_DomainOfAnotherSearch_IsAConflict()
    {
        _instances.CustomDomainTakenAsync("ostern.ceviduernten.ch", _tann.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await Set("ostern.ceviduernten.ch");

        Assert.Equal("customdomain.taken", result.Error!.Code);
    }

    [Theory]
    [InlineData("tann.oschterhas.li", "customdomain.platform_domain")]
    [InlineData("10.0.0.1", "customdomain.invalid")]
    public async Task Set_InvalidDomain_IsRejected(string domain, string code)
    {
        Assert.Equal(code, (await Set(domain)).Error!.Code);
    }

    [Fact]
    public async Task Verify_Success_SwitchesToVerified_AndPublicUrlUsesCustomDomain()
    {
        await Set("ostern.ceviduernten.ch");
        _verifier.CheckAsync("ostern.ceviduernten.ch", _tann.CustomDomainToken!, Arg.Any<CancellationToken>()).Returns(DomainCheckOutcome.Verified);
        _hostCache.ClearReceivedCalls();

        var result = (await _service.VerifyAsync(TestData.Admin, _tann.Id, TestContext.Current.CancellationToken)).Value;

        Assert.Equal(DomainCheckOutcome.Verified, result.Outcome);
        Assert.Equal(CustomDomainStatus.Verified, result.Instance.CustomDomainStatus);
        Assert.Equal("https://ostern.ceviduernten.ch", result.Instance.PublicUrl);
        Assert.Equal(_time.GetUtcNow(), _tann.CustomDomainVerifiedAt);
        _hostCache.Received(1).Invalidate();
    }

    [Fact]
    public async Task Verify_Failure_StaysPending_AndReportsOutcome()
    {
        await Set("ostern.ceviduernten.ch");
        _verifier.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(DomainCheckOutcome.Unreachable);

        var result = (await _service.VerifyAsync(TestData.Admin, _tann.Id, TestContext.Current.CancellationToken)).Value;

        Assert.Equal(DomainCheckOutcome.Unreachable, result.Outcome);
        Assert.Equal(CustomDomainStatus.Pending, _tann.CustomDomainStatus);
        Assert.Equal(1, _tann.CustomDomainFailedChecks);
    }

    [Fact]
    public async Task Verify_WithoutDomain_IsRejected()
    {
        var result = await _service.VerifyAsync(TestData.Admin, _tann.Id, TestContext.Current.CancellationToken);

        Assert.Equal("customdomain.not_set", result.Error!.Code);
    }

    [Fact]
    public async Task Remove_ClearsEverything_AndStopsServing()
    {
        await Set("ostern.ceviduernten.ch");
        _hostCache.ClearReceivedCalls();

        var dto = (await _service.RemoveAsync(TestData.Admin, _tann.Id, TestContext.Current.CancellationToken)).Value;

        Assert.Null(dto.CustomDomain);
        Assert.Equal(CustomDomainStatus.None, dto.CustomDomainStatus);
        Assert.Null(_tann.CustomDomainToken);
        _hostCache.Received(1).Invalidate();
    }

    [Fact]
    public async Task VerifiedDomain_FailsAfterThreeFailedDailyChecks_AndRecovers()
    {
        var instance = VerifiedInstance();
        _verifier.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(DomainCheckOutcome.Unreachable);
        var ct = TestContext.Current.CancellationToken;

        for (var day = 1; day <= 2; day++)
        {
            _time.Advance(TimeSpan.FromDays(1));
            await _service.CheckDueDomainsAsync(ct);
            Assert.Equal(CustomDomainStatus.Verified, instance.CustomDomainStatus);
        }

        _time.Advance(TimeSpan.FromDays(1));
        await _service.CheckDueDomainsAsync(ct);
        Assert.Equal(CustomDomainStatus.Failed, instance.CustomDomainStatus);
        _hostCache.Received(1).Invalidate();

        _verifier.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(DomainCheckOutcome.Verified);
        _time.Advance(TimeSpan.FromDays(1));
        await _service.CheckDueDomainsAsync(ct);
        Assert.Equal(CustomDomainStatus.Verified, instance.CustomDomainStatus);
        Assert.Equal(0, instance.CustomDomainFailedChecks);
    }

    [Fact]
    public async Task OneSuccess_ResetsTheFailureCount()
    {
        var instance = VerifiedInstance();
        instance.CustomDomainFailedChecks = 2;
        _verifier.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(DomainCheckOutcome.Verified);
        _time.Advance(TimeSpan.FromDays(1));

        await _service.CheckDueDomainsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, instance.CustomDomainFailedChecks);
    }

    [Fact]
    public async Task OnlyDueDomainsAreChecked()
    {
        var verified = VerifiedInstance();
        var pending = new Instance { Id = Guid.NewGuid(), Slug = "b", Name = "B", CustomDomain = "b.example.ch", CustomDomainToken = "t", CustomDomainStatus = CustomDomainStatus.Pending, CustomDomainLastCheckAt = _time.GetUtcNow() };
        _instances.ListWithCustomDomainForUpdateAsync(Arg.Any<CancellationToken>()).Returns([verified, pending]);
        _verifier.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(DomainCheckOutcome.Unreachable);
        var ct = TestContext.Current.CancellationToken;

        _time.Advance(TimeSpan.FromMinutes(9));
        Assert.Equal(0, await _service.CheckDueDomainsAsync(ct));

        _time.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(1, await _service.CheckDueDomainsAsync(ct));
        await _verifier.Received(1).CheckAsync("b.example.ch", "t", Arg.Any<CancellationToken>());
        await _verifier.DidNotReceive().CheckAsync("a.example.ch", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerificationToken_OnlyForKnownCustomDomains()
    {
        _instances.FindCustomDomainTokenAsync("ostern.ceviduernten.ch", Arg.Any<CancellationToken>()).Returns("token-1");
        var ct = TestContext.Current.CancellationToken;

        Assert.Equal("token-1", await _service.GetVerificationTokenAsync("Ostern.CeviDuernten.ch.", ct));
        Assert.Null(await _service.GetVerificationTokenAsync("andere.example.ch", ct));
        Assert.Null(await _service.GetVerificationTokenAsync("10.0.0.1", ct));
    }

    [Theory]
    [InlineData(HostKind.Instance, true)]
    [InlineData(HostKind.Admin, true)]
    [InlineData(HostKind.Landing, true)]
    [InlineData(HostKind.Unknown, false)]
    public async Task Tls_OnlyForHostsWeServe(HostKind kind, bool allowed)
    {
        _hostResolver.ResolveAsync("x.example.ch", Arg.Any<CancellationToken>()).Returns(new HostResolution(kind));

        Assert.Equal(allowed, await _service.IsTlsAllowedAsync("x.example.ch", TestContext.Current.CancellationToken));
    }

    private Task<Common.Results.Result<Oschterhas.Core.Admin.InstanceDetailDto>> Set(string domain) =>
        _service.SetAsync(TestData.Admin, _tann.Id, new SetCustomDomainRequest(domain), TestContext.Current.CancellationToken);

    private Instance VerifiedInstance()
    {
        var instance = new Instance
        {
            Id = Guid.NewGuid(),
            Slug = "a",
            Name = "A",
            CustomDomain = "a.example.ch",
            CustomDomainToken = "t",
            CustomDomainStatus = CustomDomainStatus.Verified,
            CustomDomainLastCheckAt = _time.GetUtcNow(),
        };
        _instances.ListWithCustomDomainForUpdateAsync(Arg.Any<CancellationToken>()).Returns([instance]);
        return instance;
    }
}
