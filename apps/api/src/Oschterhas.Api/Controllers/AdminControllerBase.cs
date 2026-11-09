using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oschterhas.Api.Auth;
using Oschterhas.Api.Http;
using Oschterhas.Common.Results;
using Oschterhas.Core.Users;

namespace Oschterhas.Api.Controllers;

/// <summary>
/// Base for administration endpoints: admin host only, signed in, CSRF token required for changes.
/// Permissions (platform admin, organizer of the instance) are checked in the Core services.
/// </summary>
[ApiController]
[Authorize]
[RequireAdminHost]
[AutoValidateAntiforgeryToken]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
public abstract class AdminControllerBase : ControllerBase
{
    protected CurrentUser Actor => HttpContext.GetCurrentUser();

    protected static ActionResult<T> FromResult<T>(Result<T> result) =>
        result.IsSuccess ? result.Value! : Problems.FromError(result.Error!);

    protected ActionResult FromResult(Result result) =>
        result.IsSuccess ? NoContent() : Problems.FromError(result.Error!);
}
