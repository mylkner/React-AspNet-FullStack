namespace Server.Models.Dtos;

public class RefreshTokenDto
{
    public string UserId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string TokenValue { get; set; } = string.Empty;
}
