using Microsoft.AspNetCore.Mvc;
using Oschterhas.Api.Http;
using Oschterhas.Common.Results;

namespace Oschterhas.Api.Tests.Http;

public class ProblemsTests
{
    [Theory]
    [InlineData(ErrorKind.Validation, 422)]
    [InlineData(ErrorKind.NotFound, 404)]
    [InlineData(ErrorKind.Conflict, 409)]
    [InlineData(ErrorKind.Forbidden, 403)]
    [InlineData(ErrorKind.Unexpected, 500)]
    public void MapsErrorKindToStatusAndKeepsCode(ErrorKind kind, int expectedStatus)
    {
        var result = Problems.FromError(new Error("some.code", "Some message", kind));

        Assert.Equal(expectedStatus, result.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.Equal("Some message", problem.Title);
        Assert.Equal("some.code", problem.Extensions["code"]);
        Assert.Contains("application/problem+json", result.ContentTypes);
    }
}
