using System.Security.Claims;
using JetBrains.Annotations;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Web.Helpers
{
    [UsedImplicitly]
    public class CurrentUserAccessor : ICurrentUserAccessor
    {
        public int CurrentUserId => int.Parse(ClaimsPrincipal.Current.Identity.GetUserId());
    }
}
