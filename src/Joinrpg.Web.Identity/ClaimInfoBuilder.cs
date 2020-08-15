using System.Collections.Generic;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using Claim = System.Security.Claims.Claim;

namespace Joinrpg.Web.Identity
{
    internal static class ClaimInfoBuilder
    {
        public static IList<Claim> ToClaimsList(this User dbUser)
        {
            var list = new List<Claim>();
            list.Add(new Claim(JoinClaimTypes.DisplayName, dbUser.GetDisplayName()));
            return list;
        }
    }
}
