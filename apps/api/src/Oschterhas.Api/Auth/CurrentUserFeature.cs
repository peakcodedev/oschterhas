using Oschterhas.Core.Users;

namespace Oschterhas.Api.Auth;

/// <summary>Set by the cookie validation on every authenticated request (fresh from the database).</summary>
internal sealed record CurrentUserFeature(CurrentUser User);

internal static class CurrentUserHttpContextExtensions
{
    public static CurrentUser GetCurrentUser(this HttpContext context) =>
        context.Features.Get<CurrentUserFeature>()?.User
        ?? throw new InvalidOperationException("No signed-in user. Is the endpoint missing [Authorize]?");
}
