using HelpDesk.Api.Contracts.Users;
using HelpDesk.Application.Services.Users;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserRegistrationService _userRegistrationService;

    public UsersController(UserRegistrationService userRegistrationService)
    {
        _userRegistrationService = userRegistrationService;
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Register(
        RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userRegistrationService.RegisterAsync(
            request.Firstname,
            request.Lastname,
            request.Email,
            request.Password,
            cancellationToken);

        var response = new UserResponse(user.Id,
            user.Firstname,
            user.Lastname,
            user.Email,
            user.Role.ToString());

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
