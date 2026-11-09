using Microsoft.AspNetCore.Mvc.Filters;
using Oschterhas.Common.Results;
using Oschterhas.Core.Hosting;

namespace Oschterhas.Api.Http;

/// <summary>Administration is only available on <c>admin.{BaseDomain}</c>, never on instance subdomains or custom domains.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class RequireAdminHostAttribute : ActionFilterAttribute
{
    private static readonly Error NotHere = Error.NotFound("admin.not_available", "Not available at this address.");

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.GetHostResolution().Kind != HostKind.Admin)
        {
            context.Result = Problems.FromError(NotHere);
        }
    }
}
