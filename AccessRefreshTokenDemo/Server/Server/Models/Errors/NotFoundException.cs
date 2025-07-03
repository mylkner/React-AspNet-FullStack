namespace Server.Models.Errors;

public class NotFoundException(string message) : Exception(message);
