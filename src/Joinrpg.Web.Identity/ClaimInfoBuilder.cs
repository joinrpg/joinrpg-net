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
            var claimList = new List<Claim>
            {
                new Claim(ClaimTypes.Email, dbUser.Email),
                new Claim(JoinClaimTypes.DisplayName, dbUser.GetDisplayName()),
            };
            if (dbUser.SelectedAvatarId is not null)
            {
                //TODO: When we fix all avatars, it will be not required check
                claimList.Add(new Claim(JoinClaimTypes.AvatarId, dbUser.SelectedAvatarId?.ToString()));
            }
            return claimList;
        }
    }
}
