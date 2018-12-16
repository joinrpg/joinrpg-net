using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNet.Identity;
using Claim = System.Security.Claims.Claim;

namespace Joinrpg.Web.Identity
{
    public static class ClaimUserInfoLoader
    {
        [NotNull]
        public static ICurrentUserInfo GetCurrentUserInfo(this ClaimsIdentity identity)
        {
            return new CurrentUserInfoImpl()
            {
                UserId = identity.GetUserId<int>(),
                Email = identity.GetUserName(),
                DisplayName = identity.GetValueOrDefault(ClaimTypes.Name),
            };
        }

        private static string GetValueOrDefault(this ClaimsIdentity identity, string name)
            => identity.Claims.FirstOrDefault(c => c.Type == name)?.Value;

        public static IList<Claim> ToClaimsList(this User dbUser)
        {
            var list = new List<Claim>();
            list.Add(new Claim(ClaimTypes.Name, dbUser.GetDisplayName()));
            return list;
        }
    }
}
