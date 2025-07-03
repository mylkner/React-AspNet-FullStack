using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Helpers;
using Server.Models.Db;
using Server.Models.Dtos;
using Server.Services.Interfaces;

namespace Server.Services;

public class AuthService(AppDbContext db, IConfiguration configuration) : IAuthService
{
    public async Task<User?> RegisterAsync(UserDto req)
    {
        if (await db.Users.AnyAsync(u => u.Username == req.Username))
            return null;

        string salt = AuthHelpers.GenerateRandomString(16);
        User user = new()
        {
            Username = req.Username,
            HashedPassword = Convert.ToBase64String(
                AuthHelpers.HashPassword(req.Password, Convert.FromBase64String(salt))
            ),
            Salt = salt,
            Role = "User",
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<string?> LoginAsync(UserDto req, HttpContext context)
    {
        User? user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
        if (
            user is null
            || !AuthHelpers.VerifyPassword(
                req.Password,
                user.HashedPassword,
                Convert.FromBase64String(user.Salt)
            )
        )
            return null;

        UserRefreshToken userRt = await GenerateAndSaveRefreshTokenAsync(user);
        AuthHelpers.SetRefreshCookie(
            context,
            user.Id.ToString(),
            userRt.DeviceId,
            userRt.RefreshToken
        );
        return AuthHelpers.GenerateToken(user, configuration);
    }

    public async Task<User?> LogoutAsync(HttpContext context)
    {
        User? user =
            await db
                .Users.Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u =>
                    u.Id == Guid.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier)!.Value)
                ) ?? throw new BadHttpRequestException("User not found.");

        RefreshTokenDto refreshToken = AuthHelpers.ParseRefreshToken(context)!;
        UserRefreshToken? userRefreshToken = user.RefreshTokens.FirstOrDefault(rt =>
            rt.DeviceId == refreshToken.DeviceId
        );

        if (userRefreshToken is not null)
        {
            db.UserRefreshTokens.Remove(userRefreshToken);
            await db.SaveChangesAsync();
        }

        context.Response.Cookies.Delete("refreshToken");
        return user;
    }

    public async Task<User?> DeleteAsync(HttpContext context)
    {
        User? user =
            await db.Users.FindAsync(
                Guid.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier)!.Value)
            ) ?? throw new BadHttpRequestException("User not found.");

        IQueryable<UserRefreshToken> userRefreshTokens = db.UserRefreshTokens.Where(rt =>
            rt.User.Id == user.Id
        );

        db.UserRefreshTokens.RemoveRange(userRefreshTokens);
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        context.Response.Cookies.Delete("refreshToken");
        return user;
    }

    public async Task<string?> ValidateAndReplaceRefreshTokenAsync(HttpContext context)
    {
        try
        {
            RefreshTokenDto refreshToken = AuthHelpers.ParseRefreshToken(context)!;

            User? user =
                await db
                    .Users.Include(u => u.RefreshTokens)
                    .FirstOrDefaultAsync(u => u.Id == Guid.Parse(refreshToken.UserId))
                ?? throw new BadHttpRequestException("User not found.");

            UserRefreshToken? userRefreshToken =
                user.RefreshTokens.FirstOrDefault(rt => rt.DeviceId == refreshToken.DeviceId)
                ?? throw new UnauthorizedAccessException("Missing refresh token from db.");

            if (
                userRefreshToken.Expiry <= DateTime.UtcNow
                || userRefreshToken.RefreshToken != refreshToken.TokenValue
            )
            {
                db.UserRefreshTokens.Remove(userRefreshToken);
                await db.SaveChangesAsync();
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            userRefreshToken.RefreshToken = AuthHelpers.GenerateRandomString(32);
            await db.SaveChangesAsync();
            AuthHelpers.SetRefreshCookie(
                context,
                user.Id.ToString(),
                userRefreshToken.DeviceId,
                userRefreshToken.RefreshToken
            );
            return AuthHelpers.GenerateToken(user, configuration);
        }
        catch
        {
            context.Response.Cookies.Delete("refreshToken");
            return null;
        }
    }

    private async Task<UserRefreshToken> GenerateAndSaveRefreshTokenAsync(User user)
    {
        UserRefreshToken userRefreshToken = new()
        {
            DeviceId = AuthHelpers.GenerateRandomString(16),
            RefreshToken = AuthHelpers.GenerateRandomString(32),
            Expiry = DateTime.UtcNow.AddDays(7),
            User = user,
        };

        db.UserRefreshTokens.Add(userRefreshToken);
        await db.SaveChangesAsync();
        return userRefreshToken;
    }
}
