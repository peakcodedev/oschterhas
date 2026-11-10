using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Oschterhas.Api.Http;

public sealed class InternalNetworkOptions
{
    public const string SectionName = "InternalNetwork";

    /// <summary>Networks allowed to call /internal endpoints (Caddy). Default: loopback only.</summary>
    public IList<string> AllowedNetworks { get; set; } = ["127.0.0.0/8", "::1/128"];
}

/// <summary>Endpoints for the reverse proxy only: other callers get 404 (they should not learn the endpoint exists).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class InternalOnlyAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<InternalNetworkOptions>>().Value;
        var remote = context.HttpContext.Connection.RemoteIpAddress;
        if (!IsAllowed(remote, options.AllowedNetworks))
        {
            context.Result = new NotFoundResult();
        }
    }

    internal static bool IsAllowed(IPAddress? remote, IEnumerable<string> networks)
    {
        if (remote is null)
        {
            return false;
        }

        if (remote.IsIPv4MappedToIPv6)
        {
            remote = remote.MapToIPv4();
        }

        return networks.Any(n => IPNetwork.TryParse(n, out var network)
            && network.BaseAddress.AddressFamily == remote.AddressFamily
            && network.Contains(remote));
    }
}
