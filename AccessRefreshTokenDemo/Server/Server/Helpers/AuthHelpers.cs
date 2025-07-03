using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Server.Models.Db;
using Server.Models.Dtos;

namespace Server.Helpers;

public static class AuthHelpers
{
    public static byte[] HashPassword(string password, byte[] salt)
    {
        using Rfc2898DeriveBytes pbkdf2 = new(password, salt, 100000, HashAlgorithmName.SHA512);
        return pbkdf2.GetBytes(32);
    }

    public static bool VerifyPassword(string inputtedPassword, string hashedPassword, byte[] salt)
    {
        return CryptographicOperations.FixedTimeEquals(
            HashPassword(inputtedPassword, salt),
            Convert.FromBase64String(hashedPassword)
        );
    }

    public static string GenerateRandomString(int size)
    {
        byte[] rand = new byte[size];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(rand);
        return Convert.ToBase64String(rand);
    }

    public static string GenerateToken(User user, IConfiguration configuration)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
        ];

        SigningCredentials creds = new(
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration.GetValue<string>("JWT:Key")!)
            ),
            SecurityAlgorithms.HmacSha512
        );

        JwtSecurityToken token = new(
            issuer: configuration.GetValue<string>("JWT:Issuer"),
            audience: configuration.GetValue<string>("JWT:Audience"),
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static void SetRefreshCookie(
        HttpContext context,
        string userId,
        string deviceId,
        string tokenValue
    )
    {
        context.Response.Cookies.Append(
            "refreshToken",
            $"{userId}:{deviceId}:{tokenValue}",
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
            }
        );
    }

    public static RefreshTokenDto? ParseRefreshToken(HttpContext context)
    {
        string? cookie = context.Request.Cookies["refreshToken"];
        if (cookie is null || cookie?.Split(":").Length != 3)
            throw new UnauthorizedAccessException("Cookie not found or in invalid format.");
        string[] parts = cookie.Split(":");
        return new()
        {
            UserId = parts[0],
            DeviceId = parts[1],
            TokenValue = parts[2],
        };
    }
}
