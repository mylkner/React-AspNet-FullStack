namespace Server.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public byte[] HashedPassword { get; set; } = [];
    public byte[] Salt { get; set; } = [];
}
