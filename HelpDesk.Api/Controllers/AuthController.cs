using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(
        ClaimTypes.NameIdentifier);

        var email = User.FindFirstValue(
            ClaimTypes.Email);

        var role = User.FindFirstValue(
            ClaimTypes.Role);

        return Ok(new
        {
            UserId = userId,
            Email = email,
            Role = role
        });
    }
}
