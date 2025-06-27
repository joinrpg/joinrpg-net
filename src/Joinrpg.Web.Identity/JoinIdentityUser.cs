using JoinRpg.PrimitiveTypes;

namespace Joinrpg.Web.Identity;

public class JoinIdentityUser
{
    public int Id { get; internal set; }
    public string UserName { get; set; }

    public bool HasPassword { get; internal set; }

    public bool EmaiLConfirmed { get; internal set; }

    public string? PasswordHash { get; internal set; }

    public UserDisplayName DisplayName { get; internal set; }
}
