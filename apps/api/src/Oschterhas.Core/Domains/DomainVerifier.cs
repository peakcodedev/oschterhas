using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Oschterhas.Core.Domains;

public enum DomainCheckOutcome
{
    Verified,

    /// <summary>No connection (DNS not set yet, server down, wrong port).</summary>
    Unreachable,

    /// <summary>Reached a server, but not ours (wrong token, error status).</summary>
    WrongResponse,

    /// <summary>The server answered with a redirect, which is not followed.</summary>
    Redirect,

    /// <summary>The domain points to a private or local address.</summary>
    PrivateAddress,

    Timeout,
}

/// <summary>Checks whether a custom domain points to this platform.</summary>
public interface IDomainVerifier
{
    Task<DomainCheckOutcome> CheckAsync(string domain, string expectedToken, CancellationToken cancellationToken);
}

/// <summary>
/// HTTP verification: requests <c>http://{domain}/.well-known/oschterhas-verification</c> and compares the token.
/// No redirects, short timeout, at most 1 KB is read.
/// </summary>
internal sealed class HttpDomainVerifier(HttpClient http, IOptions<CustomDomainOptions> options) : IDomainVerifier
{
    public const string VerificationPath = "/.well-known/oschterhas-verification";
    private const int MaxBodyBytes = 1024;

    public async Task<DomainCheckOutcome> CheckAsync(string domain, string expectedToken, CancellationToken cancellationToken)
    {
        var port = options.Value.VerificationPort is { } p and not 80 ? $":{p}" : string.Empty;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Value.RequestTimeout);

        try
        {
            using var response = await http.GetAsync(
                new Uri($"http://{domain}{port}{VerificationPath}"),
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token).ConfigureAwait(false);

            if ((int)response.StatusCode is >= 300 and < 400)
            {
                return DomainCheckOutcome.Redirect;
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return DomainCheckOutcome.WrongResponse;
            }

            var body = await ReadLimitedAsync(response, timeout.Token).ConfigureAwait(false);
            return Matches(body.Trim(), expectedToken) ? DomainCheckOutcome.Verified : DomainCheckOutcome.WrongResponse;
        }
        catch (HttpRequestException ex) when (ex.InnerException is PrivateAddressException)
        {
            return DomainCheckOutcome.PrivateAddress;
        }
        catch (HttpRequestException)
        {
            return DomainCheckOutcome.Unreachable;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DomainCheckOutcome.Timeout;
        }
    }

    private static async Task<string> ReadLimitedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var buffer = new byte[MaxBodyBytes];
        var total = 0;
        int read;
        while (total < buffer.Length && (read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
        }

        return Encoding.UTF8.GetString(buffer, 0, total);
    }

    private static bool Matches(string actual, string expected) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(actual), Encoding.UTF8.GetBytes(expected));

    /// <summary>
    /// Resolves the host itself and connects only to allowed addresses. Because the connection uses exactly the
    /// checked address, a DNS answer that changes between check and connect (DNS rebinding) cannot bypass it.
    /// </summary>
    internal static SocketsHttpHandler CreateHandler(CustomDomainOptions options) => new()
    {
        AllowAutoRedirect = false,
        UseProxy = false,
        UseCookies = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(1),
        ConnectCallback = async (context, cancellationToken) =>
        {
            var host = context.DnsEndPoint.Host;
            var addresses = options.AllowLocalhostDomains && (host == "localhost" || host.EndsWith(".localhost", StringComparison.Ordinal))
                ? [IPAddress.IPv6Loopback, IPAddress.Loopback]
                : await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);

            var allowed = addresses.Where(a => options.AllowPrivateAddresses || AddressPolicy.IsPublic(a)).ToArray();
            if (allowed.Length == 0)
            {
                throw new PrivateAddressException(host);
            }

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(allowed, context.DnsEndPoint.Port, cancellationToken).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        },
    };
}

internal sealed class PrivateAddressException(string host)
    : Exception($"'{host}' resolves only to private or local addresses.");
