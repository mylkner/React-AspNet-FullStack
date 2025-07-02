using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Services.Interfaces;

namespace Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<User>> Register(UserDto req)
    {
        User? user = await authService.RegisterAsync(req);
        if (user is null)
            return BadRequest("Username already exists.");

        return Ok(user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login(UserDto req)
    {
        string? token = await authService.LoginAsync(req);
        if (token is null)
            return BadRequest("Invalid username or password.");

        return Ok(token);
    }
}
