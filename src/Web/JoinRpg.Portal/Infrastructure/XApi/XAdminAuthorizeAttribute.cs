using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace JoinRpg.Portal.Infrastructure.XApi;

public class XAdminAuthorizeAttribute : AdminAuthorizeAttribute
{
    public XAdminAuthorizeAttribute() => AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme;
}
