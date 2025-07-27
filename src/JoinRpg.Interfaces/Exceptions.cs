using JoinRpg.Helpers;

namespace JoinRpg.Interfaces;

public class JoinRpgInvalidUserException(string message = "Cannot perform this operation for current user.") : JoinRpgBaseException(message)
{
}

public class MustBeAdminException : JoinRpgInvalidUserException
{
    public MustBeAdminException() : base("Cannot perform this operation for non-admin user.")
    {
    }
}
