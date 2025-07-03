namespace Server.Models.Db;

public class UserRefreshToken
{
    public Guid Id { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime Expiry { get; set; }

    public User User { get; set; } = null!;
}
