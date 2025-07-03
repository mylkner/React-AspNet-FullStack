namespace Server.Models.Errors;

public class ForbiddenException(string message) : Exception(message);
