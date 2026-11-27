using Oschterhas.Common.Results;

namespace Oschterhas.Common.Tests.Results;

public class ResultTests
{
    private static readonly Error SampleError = Error.NotFound("instance.not_found", "Instance not found");

    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_CarriesError()
    {
        Result result = SampleError;

        Assert.True(result.IsFailure);
        Assert.Equal(SampleError, result.Error);
    }

    [Fact]
    public void GenericSuccess_ReturnsValue()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_ValueThrows()
    {
        Result<int> result = SampleError;

        Assert.True(result.IsFailure);
        var ex = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Contains("instance.not_found", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Failure_WithNullError_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
        Assert.Throws<ArgumentNullException>(() => Result<int>.Failure(null!));
    }

    [Fact]
    public void ErrorFactories_SetKind()
    {
        Assert.Equal(ErrorKind.Validation, Error.Validation("c", "m").Kind);
        Assert.Equal(ErrorKind.NotFound, Error.NotFound("c", "m").Kind);
        Assert.Equal(ErrorKind.Conflict, Error.Conflict("c", "m").Kind);
        Assert.Equal(ErrorKind.Forbidden, Error.Forbidden("c", "m").Kind);
        Assert.Equal(ErrorKind.Unexpected, Error.Unexpected("c", "m").Kind);
    }
}
