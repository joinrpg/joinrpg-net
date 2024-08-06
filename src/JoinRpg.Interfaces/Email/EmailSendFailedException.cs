namespace JoinRpg.Interfaces.Email;

public class EmailSendFailedException(string message) : Exception(message)
{
}
