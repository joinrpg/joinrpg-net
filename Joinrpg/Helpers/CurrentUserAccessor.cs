using System;
using System.Security.Claims;
using JoinRpg.WebPortal.Managers.Interfaces;
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
    }
}
