using Microsoft.AspNetCore.Mvc;
using Oschterhas.Core.Users;

namespace Oschterhas.Api.Controllers;

/// <summary>Account overview for platform admins.</summary>
[Route("api/users")]
public sealed class UsersController(IUserService users) : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken cancellationToken) =>
        FromResult(await users.ListAsync(Actor, cancellationToken));

    [HttpPatch("{userId:guid}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<UserDto>> Update(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken) =>
        FromResult(await users.UpdateAsync(Actor, userId, request, cancellationToken));
}
