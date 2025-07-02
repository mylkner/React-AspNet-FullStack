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
    public async Task<ActionResult<string>> Register(LoginDto loginDto)
    {
        User? user = await authService.RegisterAsync(loginDto);
        if (user is null)
            return BadRequest("Username already exists.");

        return Ok("User registered successfully.");
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login(
        LoginDto loginDto,
        [FromHeader(Name = "Device-Id")] string deviceId
    )
    {
        TokenResponseDto? result = await authService.LoginAsync(loginDto, deviceId);
        if (result is null)
            return BadRequest("Invalid username or password.");

        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<TokenResponseDto>> RefreshToken(
        RefreshTokenDto refreshTokenDto,
        [FromHeader(Name = "Device-Id")] string deviceId
    )
    {
        TokenResponseDto? result = await authService.ValidateAndReplaceRefreshTokenAsync(
            refreshTokenDto,
            deviceId
        );
        if (result is null)
            return Unauthorized("Invalid refresh token.");

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<string>> Logout(
        string userId,
        [FromHeader(Name = "Device-Id")] string deviceId
    )
    {
        User? user = await authService.LogoutAsync(userId, deviceId);
        if (user is null)
            return BadRequest("User not found.");

        return Ok("Logged out successfully.");
    }

    [HttpPost("delete")]
    [Authorize]
    public async Task<ActionResult<string>> Delete(string userId)
    {
        User? user = await authService.DeleteAsync(userId);
        if (user is null)
            return BadRequest("User not found.");

        return Ok("User deleted successfully.");
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
