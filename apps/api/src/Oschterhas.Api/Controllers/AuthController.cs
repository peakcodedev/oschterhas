using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oschterhas.Api.Auth;
using Oschterhas.Api.Http;
using Oschterhas.Core.Users;

namespace Oschterhas.Api.Controllers;

/// <summary>Login and logout via Peakcode Identity (admin host only).</summary>
[ApiController]
[Route("api/auth")]
[RequireAdminHost]
public sealed class AuthController(IAntiforgery antiforgery, ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>Starts the login at Peakcode Identity. <paramref name="returnUrl"/> must be a local path.</summary>
    [HttpGet("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Login([FromQuery] string? returnUrl)
    {
        var target = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        return Challenge(new AuthenticationProperties { RedirectUri = target }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>The signed-in user; also issues the CSRF token cookie (XSRF-TOKEN) for the Angular app.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<MeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public ActionResult<MeDto> Me()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        Response.Cookies.Append(AuthenticationSetup.XsrfTokenCookieName, tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            SameSite = SameSiteMode.Strict,
            Secure = Request.IsHttps,
            Path = "/",
        });

        return HttpContext.GetCurrentUser().ToMeDto();
    }

    /// <summary>
    /// Ends the session here and at Peakcode Identity. Submitted as a normal form POST (with the CSRF token
    /// in the form field) so the browser can follow the redirect to Keycloak.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ValidateCsrfToken]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Logout()
    {
        logger.LogInformation("Logout: user {UserId}", HttpContext.GetCurrentUser().Id);
        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}

internal static class MeMapping
{
    public static MeDto ToMeDto(this CurrentUser user) => new(user.Id, user.Email, user.DisplayName, user.IsPlatformAdmin);
}
