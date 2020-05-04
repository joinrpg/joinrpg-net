using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces.Notification
{
    public class InviteEmailModel : EmailModelBase
    {
        public ICollection<Claim> RecipientClaims;

        public Claim GetClaimByPerson(User user)
        {
            return RecipientClaims.FirstOrDefault(claim => claim.PlayerUserId == user.UserId);
        }
    }

    public class NewInviteEmail : InviteEmailModel
    {
    }

    public class AcceptInviteEmail : InviteEmailModel
    {
    }

    public class DeclineInviteEmail : InviteEmailModel
    {
    }
}
