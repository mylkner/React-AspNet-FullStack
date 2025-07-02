using Server.Models;
using Server.Models.DTOs;

namespace Server.Services.Interfaces;

public interface IAuthService
{
    public Task<User?> RegisterAsync(UserDto req);
    public Task<TokenResponseDto?> LoginAsync(UserDto req, string deviceId);
}
