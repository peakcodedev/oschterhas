using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Filters;
using Oschterhas.Common.Results;

namespace Oschterhas.Api.Http;

/// <summary>
/// Checks the antiforgery (CSRF) token on every changing request (POST, PUT, PATCH, DELETE): header
/// <c>X-XSRF-TOKEN</c> from Angular or form field <c>__RequestVerificationToken</c> (logout form).
/// Own filter because MVC's <c>[AutoValidateAntiforgeryToken]</c> needs the Views services, which
/// <c>AddControllers()</c> does not register.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class ValidateCsrfTokenAttribute : Attribute, IAsyncAuthorizationFilter
{
    private static readonly Error Invalid = Error.Forbidden("csrf_invalid", "Security token missing or invalid. Please reload the page.");

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method))
        {
            return;
        }

        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = Problems.FromError(Invalid);
        }
    }
}
