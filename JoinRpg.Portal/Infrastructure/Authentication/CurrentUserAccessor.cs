using System;
using System.Security.Claims;
using Joinrpg.Web.Identity;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;

namespace JoinRpg.Web.Helpers
{
    /// <summary>
    /// Adapter to extract user data from HttpContext principal
    /// </summary>
    public class CurrentUserAccessor : ICurrentUserAccessor
    {
        private IHttpContextAccessor HttpContextAccessor { get; }

        private ClaimsPrincipal User => HttpContextAccessor.HttpContext.User;

        /// <summary>
        /// ctor
        /// </summary>
        public CurrentUserAccessor(IHttpContextAccessor  httpContextAccessor)
        {
            HttpContextAccessor = httpContextAccessor;
        }

        int ICurrentUserAccessor.UserId => User.GetUserIdOrDefault() ?? throw new Exception("Authorization required here");

        int? ICurrentUserAccessor.UserIdOrDefault => User.GetUserIdOrDefault();

        string ICurrentUserAccessor.DisplayName => User.FindFirst(JoinClaimTypes.DisplayName).Value;

        string ICurrentUserAccessor.Email => User.FindFirst(ClaimTypes.Email).Value;

        bool ICurrentUserAccessor.IsAdmin => User.IsInRole(DataModel.Security.AdminRoleName);
    }
}
