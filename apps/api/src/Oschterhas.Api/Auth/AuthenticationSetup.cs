using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Oschterhas.Core.Users;

namespace Oschterhas.Api.Auth;

/// <summary>
/// Backend-for-frontend login: the API runs the OpenID Connect code flow (with PKCE) against Peakcode Identity
/// and gives the browser only an httpOnly session cookie. Access and refresh tokens never reach the browser.
/// </summary>
internal static class AuthenticationSetup
{
    public const string SessionCookieName = "oschterhas_session";
    public const string AntiforgeryCookieName = "oschterhas_af";
    public const string XsrfTokenCookieName = "XSRF-TOKEN";
    public const string XsrfHeaderName = "X-XSRF-TOKEN";
    public const string UserIdClaim = "oschterhas_uid";
    public const string CallbackPath = "/api/auth/callback";

    public static IServiceCollection AddOschterhasAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var oidc = configuration.GetSection(OidcOptions.SectionName).Get<OidcOptions>() ?? new OidcOptions();
        var securePolicy = environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

        services
            .AddAuthentication(options =>
            {
                // API endpoints answer 401 instead of redirecting; only /api/auth/login starts the OIDC flow.
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                // No Domain attribute: the cookie belongs to the admin host only, instance subdomains never see it.
                options.Cookie.Name = SessionCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = securePolicy;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context => StatusOnly(context, StatusCodes.Status401Unauthorized);
                options.Events.OnRedirectToAccessDenied = context => StatusOnly(context, StatusCodes.Status403Forbidden);
                options.Events.OnValidatePrincipal = ValidatePrincipalAsync;
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = oidc.Authority;
                options.ClientId = oidc.ClientId;
                options.ClientSecret = oidc.ClientSecret;
                options.ResponseType = "code";
                // Code in the query of a normal redirect (default would be form_post: a cross-site POST that does
                // not carry the SameSite=Lax correlation and nonce cookies).
                options.ResponseMode = "query";
                options.UsePkce = true;
                options.CallbackPath = CallbackPath;
                options.SignedOutCallbackPath = "/api/auth/signout-callback";
                options.SignedOutRedirectUri = "/";
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.MapInboundClaims = false;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.SaveTokens = true;
                options.Scope.Add("email");
                options.TokenValidationParameters.NameClaimType = "name";
                // ASP.NET sets these helper cookies to SameSite=None, which browsers reject without Secure
                // (local HTTP). With ResponseMode "query" the callback is a top-level GET, so Lax is enough.
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = securePolicy;
                options.NonceCookie.SameSite = SameSiteMode.Lax;
                options.NonceCookie.SecurePolicy = securePolicy;
                options.Events.OnTokenValidated = OnTokenValidatedAsync;
                options.Events.OnRemoteFailure = context =>
                {
                    context.Response.Redirect("/?login=failed");
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        services.AddAntiforgery(options =>
        {
            options.HeaderName = XsrfHeaderName;
            options.Cookie.Name = AntiforgeryCookieName;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = securePolicy;
        });

        return services;
    }

    /// <summary>Maps the Keycloak identity to an Oschterhas user; accounts without access are sent back with a reason.</summary>
    private static async Task OnTokenValidatedAsync(TokenValidatedContext context)
    {
        var principal = context.Principal!;
        var issuer = principal.FindFirstValue("iss") ?? context.SecurityToken.Issuer;
        var subject = principal.FindFirstValue("sub");
        if (string.IsNullOrEmpty(subject))
        {
            context.Fail("Token without subject.");
            return;
        }

        var login = context.HttpContext.RequestServices.GetRequiredService<ILoginService>();
        var result = await login.SignInAsync(
            new ExternalIdentity(issuer, subject, principal.FindFirstValue("email"), principal.FindFirstValue("name")),
            context.HttpContext.RequestAborted);

        if (result.IsFailure)
        {
            var reason = result.Error!.Code.Replace("auth.", string.Empty, StringComparison.Ordinal);
            context.Response.Redirect($"/?login={Uri.EscapeDataString(reason)}");
            context.HandleResponse();
            return;
        }

        var user = result.Value;
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(UserIdClaim, user.Id.ToString()), new Claim("name", user.DisplayName), new Claim("email", user.Email)],
            CookieAuthenticationDefaults.AuthenticationScheme,
            "name",
            "role"));

        // Keep only the id token (needed as hint for the logout at Keycloak); the cookie stays small.
        var properties = context.Properties!;
        var idToken = properties.GetTokenValue("id_token");
        properties.StoreTokens(idToken is null ? [] : [new AuthenticationToken { Name = "id_token", Value = idToken }]);
    }

    /// <summary>Every request: the user must still exist and be active (disabling takes effect immediately).</summary>
    private static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
    {
        if (!Guid.TryParse(context.Principal?.FindFirstValue(UserIdClaim), out var userId))
        {
            context.RejectPrincipal();
            return;
        }

        var login = context.HttpContext.RequestServices.GetRequiredService<ILoginService>();
        var user = await login.GetActiveUserAsync(userId, context.HttpContext.RequestAborted);
        if (user is null)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        context.HttpContext.Features.Set(new CurrentUserFeature(user));
    }

    private static Task StatusOnly(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        return Task.CompletedTask;
    }
}
