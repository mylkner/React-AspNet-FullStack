using Server.Models;

namespace Server.Services.Interfaces;

public interface IAuthService
{
    public byte[] HashPassword(string password, byte[] salt);
    public bool VerifyPassword(string inputtedPassword, byte[] hashedPassword, byte[] salt);
    public byte[] GenerateSalt(int size);
    public string GenerateToken(User user);
}
