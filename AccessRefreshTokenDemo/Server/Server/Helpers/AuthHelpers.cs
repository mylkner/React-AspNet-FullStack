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
    public static byte[] HashString(string str, byte[] salt)
    {
        using Rfc2898DeriveBytes pbkdf2 = new(str, salt, 100000, HashAlgorithmName.SHA512);
        return pbkdf2.GetBytes(32);
    }

    public static bool VerifyHash(string toCompare, string ogHash, byte[] salt)
    {
        return CryptographicOperations.FixedTimeEquals(
            HashString(toCompare, salt),
            Convert.FromBase64String(ogHash)
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

    public static void SetRefreshCookie(HttpContext context, string tokenId, string tokenValue)
    {
        context.Response.Cookies.Append(
            "refreshToken",
            $"{tokenId}:{tokenValue}",
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
            }
        );
    }

    public static RefreshTokenDto ParseRefreshToken(HttpContext context)
    {
        string? cookie = context.Request.Cookies["refreshToken"];
        if (cookie is null || cookie.Split(":").Length != 2)
            throw new UnauthorizedAccessException("Cookie not provided.");
        string[] parts = cookie.Split(":");
        if (parts.Length != 2)
            throw new UnauthorizedAccessException("Cookie in invalid format.");
        return new() { TokenId = parts[0], TokenValue = parts[1] };
    }
}
