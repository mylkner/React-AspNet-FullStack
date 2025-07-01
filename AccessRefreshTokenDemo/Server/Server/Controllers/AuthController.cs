using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Services.Interfaces;
using YamlDotNet.Core.Tokens;

namespace Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, IConfiguration configuration) : ControllerBase
{
    private static User? _user;

    [HttpPost("register")]
    public ActionResult<User> Register(UserDto req)
    {
        byte[] salt = authService.GenerateSalt(16);
        byte[] hashedPassword = authService.HashPassword(req.PlainPassword, salt);
        User user = new()
        {
            Username = req.Username,
            HashedPassword = hashedPassword,
            Salt = salt,
        };
        _user = user;
        return Ok(user);
    }

    [HttpPost("login")]
    public ActionResult<string> Login(UserDto req)
    {
        if (
            _user.Username != req.Username
            || !authService.VerifyPassword(req.PlainPassword, _user.HashedPassword, _user.Salt)
        )
            return BadRequest("Invalid username or password");

        string token = authService.GenerateToken(_user);
        return Ok(token);
    }
}
