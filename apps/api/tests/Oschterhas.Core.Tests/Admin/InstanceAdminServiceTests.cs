using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Oschterhas.Core.Admin;
using Oschterhas.Core.Events;
using Oschterhas.Core.Hosting;
using Oschterhas.Data.Entities;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Tests.Admin;

public class InstanceAdminServiceTests
{
    private readonly IInstanceRepository _instances = Substitute.For<IInstanceRepository>();
    private readonly IHostCacheInvalidator _hostCache = Substitute.For<IHostCacheInvalidator>();
    private readonly IInstanceEventBroadcaster _events = Substitute.For<IInstanceEventBroadcaster>();
    private readonly InstanceAdminService _service;
    private readonly Instance _tann = TestData.Tann();

    public InstanceAdminServiceTests()
    {
        _instances.FindForUpdateAsync(_tann.Id, Arg.Any<CancellationToken>()).Returns(_tann);
        _instances.SlugExistsAsync("tann", Arg.Any<CancellationToken>()).Returns(true);
        var hosting = Options.Create(new HostingOptions { BaseDomain = "oschterhas.li", PublicScheme = "https" });
        _service = new InstanceAdminService(_instances, _hostCache, _events, hosting, new FakeTimeProvider());
    }

    [Fact]
    public async Task List_AdminSeesAll_OrganizerSeesOwn()
    {
        _instances.ListAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns([_tann]);
        var ct = TestContext.Current.CancellationToken;

        await _service.ListAsync(TestData.Admin, ct);
        await _service.ListAsync(TestData.Otto, ct);

        await _instances.Received(1).ListAsync(null, Arg.Any<CancellationToken>());
        await _instances.Received(1).ListAsync(TestData.Otto.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_OrganizerOfOtherInstance_GetsNotFound()
    {
        var result = await _service.GetAsync(TestData.Stranger, _tann.Id, TestContext.Current.CancellationToken);

        Assert.Equal("instance.not_found", result.Error!.Code);
    }

    [Fact]
    public async Task Get_ShowsPublicUrlAndOrganizers()
    {
        var dto = (await _service.GetAsync(TestData.Otto, _tann.Id, TestContext.Current.CancellationToken)).Value;

        Assert.Equal("https://tann.oschterhas.li", dto.PublicUrl);
        Assert.Equal("Otto", Assert.Single(dto.Organizers).DisplayName);
    }

    [Fact]
    public async Task PublicUrl_PrefersVerifiedCustomDomain()
    {
        _tann.CustomDomain = "ostern.ceviduernten.ch";
        _tann.CustomDomainStatus = CustomDomainStatus.Verified;

        var dto = (await _service.GetAsync(TestData.Admin, _tann.Id, TestContext.Current.CancellationToken)).Value;

        Assert.Equal("https://ostern.ceviduernten.ch", dto.PublicUrl);
        Assert.Equal("https://tann.oschterhas.li", dto.SubdomainUrl);
    }

    [Fact]
    public async Task Create_OnlyByPlatformAdmin()
    {
        var result = await _service.CreateAsync(TestData.Otto, new("neu", "Neu", null, null), TestContext.Current.CancellationToken);

        Assert.Equal("auth.platform_admin_required", result.Error!.Code);
        _instances.DidNotReceive().Add(Arg.Any<Instance>());
    }

    [Fact]
    public async Task Create_NormalizesSlug_StartsAsDraft_AndInvalidatesHostCache()
    {
        var dto = (await _service.CreateAsync(TestData.Admin, new(" Duernten ", " Ostersuche Dürnten ", "  ", new DateOnly(2027, 3, 28)), TestContext.Current.CancellationToken)).Value;

        Assert.Equal("duernten", dto.Slug);
        Assert.Equal("Ostersuche Dürnten", dto.Name);
        Assert.Null(dto.Description);
        Assert.Equal(InstanceStatus.Draft, dto.Status);
        _instances.Received(1).Add(Arg.Is<Instance>(i => i.Slug == "duernten"));
        _hostCache.Received(1).Invalidate();
    }

    [Theory]
    [InlineData("tann", "Name", "instance.slug_taken")]
    [InlineData("admin", "Name", "instance.slug_reserved")]
    [InlineData("x", "Name", "instance.slug_invalid")]
    [InlineData("neu", "  ", "instance.name_required")]
    public async Task Create_ValidatesInput(string slug, string name, string expectedCode)
    {
        var result = await _service.CreateAsync(TestData.Admin, new(slug, name, null, null), TestContext.Current.CancellationToken);

        Assert.Equal(expectedCode, result.Error!.Code);
    }

    [Fact]
    public async Task Create_RaceOnSlug_IsAConflict()
    {
        _instances.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new UniqueConstraintException("ix_instances_slug", new InvalidOperationException())));

        var result = await _service.CreateAsync(TestData.Admin, new("neu", "Neu", null, null), TestContext.Current.CancellationToken);

        Assert.Equal("instance.slug_taken", result.Error!.Code);
    }

    [Fact]
    public async Task Update_OrganizerMayChangeTexts_ButNotTheSlug()
    {
        var ct = TestContext.Current.CancellationToken;

        var texts = await _service.UpdateAsync(TestData.Otto, _tann.Id, new("tann", "Neuer Name", "Beschreibung", null), ct);
        var slug = await _service.UpdateAsync(TestData.Otto, _tann.Id, new("anders", "Neuer Name", null, null), ct);

        Assert.Equal("Neuer Name", texts.Value.Name);
        Assert.Equal("instance.slug_change_forbidden", slug.Error!.Code);
        Assert.Equal("tann", _tann.Slug);
        _events.Received(1).Publish(_tann.Id, Arg.Is<InstanceEvent>(e => e.Type == InstanceEvent.InstanceStatus));
    }

    [Fact]
    public async Task Update_AdminChangesSlug_AndHostCacheIsInvalidated()
    {
        var dto = (await _service.UpdateAsync(TestData.Admin, _tann.Id, new("tann-2027", "Tann", null, null), TestContext.Current.CancellationToken)).Value;

        Assert.Equal("tann-2027", dto.Slug);
        _hostCache.Received(1).Invalidate();
    }

    [Fact]
    public async Task SetStatus_ByOrganizer_NotifiesSearchers()
    {
        var dto = (await _service.SetStatusAsync(TestData.Otto, _tann.Id, new(InstanceStatus.Active), TestContext.Current.CancellationToken)).Value;

        Assert.Equal(InstanceStatus.Active, dto.Status);
        _events.Received(1).Publish(_tann.Id, Arg.Is<InstanceEvent>(e => e.Type == InstanceEvent.InstanceStatus));
    }

    [Fact]
    public async Task SetStatus_RejectsUnknownValue()
    {
        var result = await _service.SetStatusAsync(TestData.Admin, _tann.Id, new((InstanceStatus)42), TestContext.Current.CancellationToken);

        Assert.Equal("instance.status_invalid", result.Error!.Code);
    }

    [Fact]
    public async Task Delete_OnlyByPlatformAdmin()
    {
        var ct = TestContext.Current.CancellationToken;

        Assert.Equal("auth.platform_admin_required", (await _service.DeleteAsync(TestData.Otto, _tann.Id, ct)).Error!.Code);
        Assert.True((await _service.DeleteAsync(TestData.Admin, _tann.Id, ct)).IsSuccess);
        _instances.Received(1).Remove(_tann);
        _hostCache.Received(1).Invalidate();
    }

    [Theory]
    [InlineData("neu", true, null)]
    [InlineData("Tann", false, "instance.slug_taken")]
    [InlineData("www", false, "instance.slug_reserved")]
    public async Task CheckSlug(string slug, bool available, string? reason)
    {
        var dto = (await _service.CheckSlugAsync(TestData.Admin, slug, TestContext.Current.CancellationToken)).Value;

        Assert.Equal(available, dto.Available);
        Assert.Equal(reason, dto.Reason);
    }
}
