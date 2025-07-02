namespace Server.Models.DTOs
{
    public class RefreshTokenRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
