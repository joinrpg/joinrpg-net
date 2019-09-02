using JoinRpg.DataModel;
using Microsoft.AspNetCore.Authorization;

namespace JoinRpg.Portal.Infrastructure.Authorization
{
    public class AdminAuthorizeAttribute : AuthorizeAttribute
    {
        public AdminAuthorizeAttribute()
        {
            Roles = Security.AdminRoleName;
        }
    }
}
