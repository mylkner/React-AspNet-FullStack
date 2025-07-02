using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Data;
using Server.Models;
using Server.Services.Interfaces;

namespace Server.Services;

public class AuthService(AppDbContext db, IConfiguration configuration) : IAuthService
{
    public async Task<User?> RegisterAsync(UserDto req)
    {
        if (await db.Users.AnyAsync(u => u.Username == req.Username))
            return null;

        string salt = GenerateSalt(16);
        User user = new()
        {
            Username = req.Username,
            HashedPassword = HashPassword(req.PlainPassword, Convert.FromBase64String(salt)),
            Salt = salt,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    public async Task<string?> LoginAsync(UserDto req)
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

        return GenerateToken(user);
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

    private static string GenerateRefreshToken()
    {
        byte[] randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private static byte[] HashPassword(string password, byte[] salt)
    {
        using Rfc2898DeriveBytes pbkdf2 = new(password, salt, 100000, HashAlgorithmName.SHA512);
        return pbkdf2.GetBytes(32);
    }

    private static bool VerifyPassword(string inputtedPassword, byte[] hashedPassword, byte[] salt)
    {
        return CryptographicOperations.FixedTimeEquals(
            HashPassword(inputtedPassword, salt),
            hashedPassword
        );
    }

    private static string GenerateSalt(int size)
    {
        byte[] salt = new byte[size];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return Convert.ToBase64String(salt);
    }
}
