using JoinRpg.Portal.Infrastructure.Authentication;

namespace JoinRpg.Portal.Controllers.Common;

/// <summary>
/// This controller is used if we need to access user methods.
/// It's recommended that you inject ICurrentUserAccessor in your constructor
/// </summary>
[Obsolete]
public abstract class LegacyJoinControllerBase : ControllerBase
{

    protected int CurrentUserId
    {
        get
        {
            var id = CurrentUserIdOrDefault;
            if (id == null)
            {
                throw new Exception("Authorization required here");
            }

            return id.Value;
        }
    }

    protected int? CurrentUserIdOrDefault => User.GetUserIdOrDefault();

}
