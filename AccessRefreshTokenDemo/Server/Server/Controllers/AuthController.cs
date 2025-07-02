using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Models.Dtos.AuthDtos;
using Server.Services.Interfaces;

namespace Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<string>> Register(RegisterDto registerDto)
    {
        User? user = await authService.RegisterAsync(registerDto);
        if (user is null)
            return BadRequest("Username already exists.");

        return Created();
    }

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login(LoginDto loginDto)
    {
        string? jwt = await authService.LoginAsync(loginDto, HttpContext);
        if (jwt is null)
            return BadRequest("Invalid username or password.");

        return Ok(jwt);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<string>> Logout(UserDeviceIdsDto userDeviceIdsDto)
    {
        User? user = await authService.LogoutAsync(userDeviceIdsDto, HttpContext);
        if (user is null)
            return BadRequest("Invalid user ID.");

        return NoContent();
    }

    [HttpDelete("delete")]
    [Authorize]
    public async Task<ActionResult<string>> Delete(DeleteDto deleteDto)
    {
        User? user = await authService.DeleteAsync(deleteDto, HttpContext);
        if (user is null)
            return BadRequest("Invalid user ID.");

        return NoContent();
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<string>> RefreshToken(UserDeviceIdsDto userDeviceIdsDto)
    {
        string? jwt = await authService.ValidateAndReplaceRefreshTokenAsync(
            userDeviceIdsDto,
            HttpContext
        );
        if (jwt is null)
            return Unauthorized("Invalid refresh token.");

        return Ok(jwt);
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
