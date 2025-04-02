namespace JoinRpg.Helpers;
public abstract class JoinRpgBaseException(Exception? inner, string message) : ApplicationException(message, inner)
{
    protected JoinRpgBaseException(string message) : this(inner: null, message)
    {

    }
}
