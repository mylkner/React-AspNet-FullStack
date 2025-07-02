using Server.Models;
using Server.Models.Dtos.AuthDtos;

namespace Server.Services.Interfaces;

public interface IAuthService
{
    public Task<User?> RegisterAsync(LoginDto req);
    public Task<TokenResponseDto?> LoginAsync(LoginDto req, string deviceId);
    public Task<TokenResponseDto?> ValidateAndReplaceRefreshTokenAsync(
        RefreshTokenDto req,
        string deviceId
    );
    public Task<User?> LogoutAsync(string userId, string deviceId);
    public Task<User?> DeleteAsync(string userId);
}
