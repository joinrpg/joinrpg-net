using System.Collections.Generic;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using Claim = System.Security.Claims.Claim;
using ClaimTypes = System.Security.Claims.ClaimTypes;

namespace Joinrpg.Web.Identity
{
    internal static class ClaimInfoBuilder
    {
        public static IList<Claim> ToClaimsList(this User dbUser)
        {
            return new List<Claim>
            {
                new Claim(ClaimTypes.Email, dbUser.Email),
                new Claim(JoinClaimTypes.DisplayName, dbUser.GetDisplayName()),
                new Claim(JoinClaimTypes.AvatarId, dbUser.SelectedAvatarId?.ToString())
            };
        }
    }
}
