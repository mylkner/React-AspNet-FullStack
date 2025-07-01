namespace Server.Services.Interfaces;

public interface IPasswordService
{
    public byte[] HashPassword(string password, byte[] salt);
    public bool VerifyPassword(string inputtedPassword, byte[] hashedPassword, byte[] salt);
    public byte[] GenerateSalt(int size);
}
