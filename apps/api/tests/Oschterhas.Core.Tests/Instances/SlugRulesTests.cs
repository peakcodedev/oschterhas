using Oschterhas.Core.Instances;

namespace Oschterhas.Core.Tests.Instances;

public class SlugRulesTests
{
    [Theory]
    [InlineData("duernten", "duernten")]
    [InlineData("  Tann ", "tann")]
    [InlineData("cevi-duernten-2027", "cevi-duernten-2027")]
    [InlineData("abc", "abc")]
    [InlineData("a1b", "a1b")]
    public void Normalize_AcceptsValidSlugs(string input, string expected)
    {
        var result = SlugRules.Normalize(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData(null, "instance.slug_required")]
    [InlineData("", "instance.slug_required")]
    [InlineData("   ", "instance.slug_required")]
    [InlineData("ab", "instance.slug_invalid")]
    [InlineData("-tann", "instance.slug_invalid")]
    [InlineData("tann-", "instance.slug_invalid")]
    [InlineData("dürnten", "instance.slug_invalid")]
    [InlineData("tann.ch", "instance.slug_invalid")]
    [InlineData("tann_1", "instance.slug_invalid")]
    [InlineData("admin", "instance.slug_reserved")]
    [InlineData("WWW", "instance.slug_reserved")]
    [InlineData("custom", "instance.slug_reserved")]
    public void Normalize_RejectsInvalidSlugs(string? input, string expectedCode)
    {
        var result = SlugRules.Normalize(input);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error!.Code);
    }

    [Fact]
    public void Normalize_RejectsTooLongSlug()
    {
        var result = SlugRules.Normalize(new string('a', 41));

        Assert.Equal("instance.slug_invalid", result.Error!.Code);
    }

    [Fact]
    public void Normalize_AcceptsMaximumLength()
    {
        Assert.True(SlugRules.Normalize(new string('a', 40)).IsSuccess);
    }
}
