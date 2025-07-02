namespace Server.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string HashedPassword { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public List<UserRefreshToken> RefreshTokens { get; set; } = [];
}
