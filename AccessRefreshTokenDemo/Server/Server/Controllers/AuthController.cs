using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Services.Interfaces;

namespace Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IPasswordService passwordService) : ControllerBase
{
    private readonly IPasswordService _passwordService = passwordService;

    [HttpPost("register")]
    public ActionResult<User> Register(UserDto req)
    {
        byte[] salt = _passwordService.GenerateSalt(16);
        byte[] hashedPassword = _passwordService.HashPassword(req.PlainPassword, salt);
        User user = new()
        {
            Username = req.Username,
            HashedPassword = hashedPassword,
            Salt = salt,
        };

        return Ok(user);
    }
}
