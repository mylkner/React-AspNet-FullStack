namespace Server.Models.DTOs;

public class UserDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PlainPassword { get; set; } = string.Empty;
}
