namespace JoinRpg.Services.Interfaces.Email;

public class EmailSendFailedException : Exception
{
    public EmailSendFailedException(string message) : base(message)
    {
    }
}
