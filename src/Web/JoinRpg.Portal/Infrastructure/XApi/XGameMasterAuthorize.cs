using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace JoinRpg.Portal.Infrastructure.Authorization;

// TODO sufficient?
public class XGameMasterAuthorize : MasterAuthorize
{
    public XGameMasterAuthorize()
    {
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme;
    }
}
