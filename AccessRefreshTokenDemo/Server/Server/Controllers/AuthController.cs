using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Models.DTOs;
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
    public async Task<ActionResult<TokenResponseDto>> Login(
        UserDto req,
        [FromHeader(Name = "Device-Id")] string deviceId
    )
    {
        TokenResponseDto? result = await authService.LoginAsync(req, deviceId);
        if (result is null)
            return BadRequest("Invalid username or password.");

        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto req)
    {
        TokenResponseDto? result = await authService.ValidateAndReplaceRefreshTokenAsync(req);
        if (result is null)
            return Unauthorized("Invalid refresh token.");

        return Ok(result);
    }

    [HttpGet]
    [Authorize]
    public IActionResult AuthEndpoint()
    {
        return Ok("This is the auth endpoint.");
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminEndpoint()
    {
        return Ok("This is the admin endpoint.");
    }
}
