using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Authorization;

namespace JoinRpg.Portal.Infrastructure.Authorization
{
    public class RequireMasterOrAdmin : AuthorizeAttribute
    {
        public RequireMasterOrAdmin(Permission permission = Permission.None) : base($"{nameof(RequireMasterOrAdmin)}__{permission}") { }
    }

    public class RequireMasterOrPublish : AuthorizeAttribute
    {
        public RequireMasterOrPublish(Permission permission = Permission.None) : base($"{nameof(RequireMasterOrPublish)}__{permission}") { }
    }

    public class RequireMaster: AuthorizeAttribute
    {
        public RequireMaster(Permission permission = Permission.None) : base($"{nameof(RequireMaster)}__{permission}") { }
    }


    public class MasterAuthorize : RequireMaster
    {

        public MasterAuthorize(Permission permission = Permission.None) : base(permission)
        {
        }
    }
}
