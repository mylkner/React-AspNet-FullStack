using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Data;
using Server.Models;
using Server.Models.Dtos.AuthDtos;
using Server.Services.Interfaces;

namespace Server.Services;

public class AuthService(AppDbContext db, IConfiguration configuration) : IAuthService
{
    public async Task<User?> RegisterAsync(LoginDto req)
    {
        if (await db.Users.AnyAsync(u => u.Username == req.Username))
            return null;

        string salt = GenerateRandomString(16);
        User user = new()
        {
            Username = req.Username,
            HashedPassword = Convert.ToBase64String(
                HashPassword(req.Password, Convert.FromBase64String(salt))
            ),
            Salt = salt,
            Role = "User",
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    public async Task<TokenResponseDto?> LoginAsync(LoginDto req, string deviceId)
    {
        User? user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
        if (
            user is null
            || !VerifyPassword(
                req.Password,
                user.HashedPassword,
                Convert.FromBase64String(user.Salt)
            )
        )
            return null;

        return new()
        {
            AccessToken = GenerateToken(user),
            RefreshToken = await GenerateAndSaveRefreshTokenAsync(user, deviceId),
        };
    }

    public async Task<TokenResponseDto?> ValidateAndReplaceRefreshTokenAsync(
        RefreshTokenDto req,
        string deviceId
    )
    {
        if (!Guid.TryParse(req.UserId, out Guid userGuid))
            return null;

        User? user = await db
            .Users.Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userGuid);

        if (user is null)
            return null;

        UserRefreshToken? userRefreshToken = user.RefreshTokens.FirstOrDefault(rt =>
            rt.DeviceId == deviceId && rt.RefreshToken == req.RefreshToken
        );

        if (userRefreshToken is null)
            return null;

        if (userRefreshToken.Expiry <= DateTime.UtcNow)
        {
            db.UserRefreshTokens.Remove(userRefreshToken);
            await db.SaveChangesAsync();
            return null;
        }

        userRefreshToken.RefreshToken = GenerateRandomString(32);
        await db.SaveChangesAsync();

        return new()
        {
            AccessToken = GenerateToken(user),
            RefreshToken = userRefreshToken.RefreshToken,
        };
    }

    public async Task<User?> LogoutAsync(string userId, string deviceId)
    {
        if (!Guid.TryParse(userId, out Guid userGuid))
            return null;

        User? user = await db
            .Users.Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userGuid);

        if (user is null)
            return null;

        UserRefreshToken? userRefreshToken = user.RefreshTokens.FirstOrDefault(rt =>
            rt.DeviceId == deviceId
        );

        if (userRefreshToken is not null)
        {
            db.UserRefreshTokens.Remove(userRefreshToken);
            await db.SaveChangesAsync();
        }

        return user;
    }

    public async Task<User?> DeleteAsync(string userId)
    {
        if (!Guid.TryParse(userId, out Guid userGuid))
            return null;

        User? user = await db.Users.FindAsync(userGuid);

        if (user is null)
            return null;

        IQueryable<UserRefreshToken> userRefreshTokens = db.UserRefreshTokens.Where(rt =>
            rt.User.Id == user.Id
        );

        db.UserRefreshTokens.RemoveRange(userRefreshTokens);
        db.Users.Remove(user);
        await db.SaveChangesAsync();

        return user;
    }

    private async Task<string> GenerateAndSaveRefreshTokenAsync(User user, string deviceId)
    {
        string refreshToken = GenerateRandomString(32);

        UserRefreshToken userRefreshToken = new()
        {
            DeviceId = deviceId,
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
