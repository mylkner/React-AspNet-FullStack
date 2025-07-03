namespace Server.Models.Errors;

public class RefreshTokenError(Exception originalEx) : Exception(originalEx.Message)
{
    public Exception OriginalEx { get; set; } = originalEx;
}
