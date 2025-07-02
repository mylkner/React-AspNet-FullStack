using Server.Models;

namespace Server.Services.Interfaces;

public interface IAuthService
{
    public Task<User?> RegisterAsync(UserDto req);
    public Task<TokenResponseDto?> LoginAsync(UserDto req);
}
