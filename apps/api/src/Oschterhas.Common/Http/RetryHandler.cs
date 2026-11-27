using System.Net;
using Oschterhas.Common.Resilience;

namespace Oschterhas.Common.Http;

/// <summary>
/// Retries idempotent HTTP requests (GET, HEAD, PUT, DELETE, OPTIONS) on network errors, 5xx and 429.
/// POST is never repeated, because it could create something twice. Replaces Polly (see docs/lizenzen.md).
/// </summary>
public sealed class RetryHandler(RetryOptions options, TimeProvider timeProvider) : DelegatingHandler
{
    private static readonly HashSet<HttpMethod> IdempotentMethods =
        [HttpMethod.Get, HttpMethod.Head, HttpMethod.Put, HttpMethod.Delete, HttpMethod.Options];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!IdempotentMethods.Contains(request.Method))
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        // Buffer the body once so it can be sent again.
        if (request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
        }

        var retryOptions = options with { ShouldRetry = ex => ex is HttpRequestException or TransientHttpException };
        return await Retry.ExecuteAsync(
            async ct =>
            {
                var response = await base.SendAsync(request, ct).ConfigureAwait(false);
                if (IsTransient(response.StatusCode))
                {
                    response.Dispose();
                    throw new TransientHttpException(response.StatusCode);
                }

                return response;
            },
            retryOptions,
            timeProvider,
            cancellationToken).ConfigureAwait(false);
    }

    private static bool IsTransient(HttpStatusCode status) =>
        status == HttpStatusCode.TooManyRequests || (int)status >= 500;
}

/// <summary>A 5xx or 429 response that is still failing after all attempts.</summary>
public sealed class TransientHttpException(HttpStatusCode statusCode)
    : HttpRequestException($"HTTP {(int)statusCode} after all retry attempts.", null, statusCode);
