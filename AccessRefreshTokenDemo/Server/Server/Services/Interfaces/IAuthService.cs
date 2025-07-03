using Server.Models.Db;
using Server.Models.Dtos;

namespace Server.Services.Interfaces;

public interface IAuthService
{
    public Task<User?> RegisterAsync(UserDto req);
    public Task<string?> LoginAsync(UserDto req, HttpContext context);
    public Task<User?> LogoutAsync(HttpContext context);
    public Task<User?> DeleteAsync(HttpContext context);
    public Task<string?> ValidateAndReplaceRefreshTokenAsync(HttpContext context);
}
