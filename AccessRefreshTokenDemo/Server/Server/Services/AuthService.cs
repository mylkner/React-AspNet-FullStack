using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Helpers;
using Server.Models.Db;
using Server.Models.Dtos;
using Server.Models.Errors;
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
                AuthHelpers.HashString(req.Password, Convert.FromBase64String(salt))
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
            || !AuthHelpers.VerifyHash(
                req.Password,
                user.HashedPassword,
                Convert.FromBase64String(user.Salt)
            )
        )
            return null;

        UserRefreshToken userRt = await GenerateAndSaveRefreshTokenAsync(user, context);
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
        UserRefreshToken? userRefreshToken = user.RefreshTokens.Find(rt =>
            rt.Id == Guid.Parse(refreshToken.TokenId)
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
            await db
                .Users.Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u =>
                    u.Id == Guid.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier)!.Value)
                ) ?? throw new BadHttpRequestException("User not found.");

        db.UserRefreshTokens.RemoveRange(user.RefreshTokens);
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

            UserRefreshToken? userRefreshToken =
                await db
                    .UserRefreshTokens.Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Id == Guid.Parse(refreshToken.TokenId))
                ?? throw new UnauthorizedAccessException("Missing refresh token from db.");

            db.UserRefreshTokens.Remove(userRefreshToken);
            if (
                userRefreshToken.Expiry <= DateTime.UtcNow
                || !AuthHelpers.VerifyHash(
                    refreshToken.TokenValue,
                    userRefreshToken.RefreshToken,
                    Convert.FromBase64String(userRefreshToken.Salt)
                )
            )
            {
                await db.SaveChangesAsync();
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }
            await GenerateAndSaveRefreshTokenAsync(userRefreshToken.User, context);
            return AuthHelpers.GenerateToken(userRefreshToken.User, configuration);
        }
        catch (Exception ex)
        {
            throw new RefreshTokenError(ex);
        }
    }

    private async Task<UserRefreshToken> GenerateAndSaveRefreshTokenAsync(
        User user,
        HttpContext context
    )
    {
        string salt = AuthHelpers.GenerateRandomString(16);
        string refreshTokenPlain = AuthHelpers.GenerateRandomString(32);
        byte[] hashedRefreshToken = AuthHelpers.HashString(
            refreshTokenPlain,
            Convert.FromBase64String(salt)
        );

        UserRefreshToken userRefreshToken = new()
        {
            RefreshToken = Convert.ToBase64String(hashedRefreshToken),
            Salt = salt,
            Expiry = DateTime.UtcNow.AddDays(7),
            User = user,
        };

        db.UserRefreshTokens.Add(userRefreshToken);
        await db.SaveChangesAsync();
        AuthHelpers.SetRefreshCookie(context, userRefreshToken.Id.ToString(), refreshTokenPlain);
        return userRefreshToken;
    }
}
