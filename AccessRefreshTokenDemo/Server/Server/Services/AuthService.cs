using System.Security.Cryptography;
using Server.Services.Interfaces;

namespace Server.Services;

public class AuthService : IAuthService
{
    public byte[] HashPassword(string password, byte[] salt)
    {
        using Rfc2898DeriveBytes pbkdf2 = new(password, salt, 100000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32);
    }

    public bool VerifyPassword(string inputtedPassword, byte[] hashedPassword, byte[] salt)
    {
        return CryptographicOperations.FixedTimeEquals(
            HashPassword(inputtedPassword, salt),
            hashedPassword
        );
    }

    public byte[] GenerateSalt(int size)
    {
        byte[] salt = new byte[size];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }
}
