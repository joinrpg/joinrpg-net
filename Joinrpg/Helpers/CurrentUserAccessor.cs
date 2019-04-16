using System;
using System.Linq;
using System.Security.Claims;
using Joinrpg.Web.Identity;
using JoinRpg.Interfaces;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Web.Helpers
{
    public class CurrentUserAccessor : ICurrentUserAccessor
    {
        public int UserId => UserIdOrDefault ?? throw new Exception("Authorization required here");

        public int? UserIdOrDefault
        {
            get
            {
                var userIdString = ClaimsPrincipal.Current.Identity.GetUserId();
                if (userIdString == null)
                {
                    return null;
                }
                else
                {
                    return int.TryParse(userIdString, out var i) ? (int?)i : null;
                }
            }
        }

        string ICurrentUserAccessor.DisplayName => ClaimsPrincipal.Current.Claims.First(c => c.Type == JoinClaimTypes.DisplayName)?.Value;

        string ICurrentUserAccessor.Email => ClaimsPrincipal.Current.Identity.GetUserName();

        bool ICurrentUserAccessor.IsAdmin => ClaimsPrincipal.Current.IsInRole(DataModel.Security.AdminRoleName);
    }
}
