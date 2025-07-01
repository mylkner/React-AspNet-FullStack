using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Server.Models;
using Server.Services.Interfaces;

namespace Server.Services;

public class AuthService(IConfiguration configuration) : IAuthService
{
    public byte[] HashPassword(string password, byte[] salt)
    {
        using Rfc2898DeriveBytes pbkdf2 = new(password, salt, 100000, HashAlgorithmName.SHA512);
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

    public string GenerateToken(User user)
    {
        List<Claim> claims = [new Claim(ClaimTypes.Name, user.Username)];

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
}
