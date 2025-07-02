using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Data;
using Server.Models;
using Server.Models.DTOs;
using Server.Services.Interfaces;

namespace Server.Services;

public class AuthService(AppDbContext db, IConfiguration configuration) : IAuthService
{
    public async Task<User?> RegisterAsync(UserDto req)
    {
        if (await db.Users.AnyAsync(u => u.Username == req.Username))
            return null;

        string salt = GenerateRandomString(16);
        User user = new()
        {
            Username = req.Username,
            HashedPassword = Convert.ToBase64String(
                HashPassword(req.PlainPassword, Convert.FromBase64String(salt))
            ),
            Salt = salt,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    public async Task<TokenResponseDto?> LoginAsync(UserDto req)
    {
        User? user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
        if (
            user is null
            || !VerifyPassword(
                req.PlainPassword,
                user.HashedPassword,
                Convert.FromBase64String(user.Salt)
            )
        )
            return null;

        return new()
        {
            AccessToken = GenerateToken(user),
            RefreshToken = await GenerateAndSaveRefreshTokenAsync(user),
        };
    }

    private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
    {
        string refreshToken = GenerateRandomString(32);

        UserRefreshToken userRefreshToken = new()
        {
            DeviceId = Guid.NewGuid().ToString(),
            RefreshToken = refreshToken,
            Expiry = DateTime.UtcNow.AddDays(7),
            User = user,
        };

        db.UserRefreshTokens.Add(userRefreshToken);
        await db.SaveChangesAsync();
        return refreshToken;
    }

    private string GenerateToken(User user)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
        ];

        SigningCredentials creds = new(
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSettings:Key")!)
            ),
            SecurityAlgorithms.HmacSha512
        );

        JwtSecurityToken token = new(
            issuer: configuration.GetValue<string>("AppSettings:Issuer"),
            audience: configuration.GetValue<string>("AppSettings:Audience"),
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static byte[] HashPassword(string password, byte[] salt)
    {
        using Rfc2898DeriveBytes pbkdf2 = new(password, salt, 100000, HashAlgorithmName.SHA512);
        return pbkdf2.GetBytes(32);
    }

    private static bool VerifyPassword(string inputtedPassword, string hashedPassword, byte[] salt)
    {
        return CryptographicOperations.FixedTimeEquals(
            HashPassword(inputtedPassword, salt),
            Convert.FromBase64String(hashedPassword)
        );
    }

    private static string GenerateRandomString(int size)
    {
        byte[] rand = new byte[size];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(rand);
        return Convert.ToBase64String(rand);
    }
}
