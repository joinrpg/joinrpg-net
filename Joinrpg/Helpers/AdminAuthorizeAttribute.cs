using System.Web.Mvc;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Helpers
{
  public class AdminAuthorizeAttribute : AuthorizeAttribute
  {
    public AdminAuthorizeAttribute()
    {
      Roles = Security.AdminRoleName;
    }
  }
}