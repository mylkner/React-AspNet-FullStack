using Server.Models;
using Server.Models.Dtos.AuthDtos;

namespace Server.Services.Interfaces;

public interface IAuthService
{
    public Task<User?> RegisterAsync(RegisterDto req);
    public Task<string?> LoginAsync(LoginDto req);
    public Task<string?> ValidateAndReplaceRefreshTokenAsync(UserDeviceIdsDto req);
    public Task<User?> LogoutAsync(UserDeviceIdsDto req);
    public Task<User?> DeleteAsync(DeleteDto req);
}
