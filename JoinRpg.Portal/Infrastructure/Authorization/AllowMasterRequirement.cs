using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Authorization;

namespace JoinRpg.Portal.Infrastructure.Authorization
{
    public class AllowMasterRequirement : IAuthorizationRequirement
    {
        public Permission Permission { get; set; }
    }
}
