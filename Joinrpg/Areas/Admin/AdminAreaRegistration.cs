using System.Web.Mvc;
using JetBrains.Annotations;

namespace JoinRpg.Web.Areas.Admin
{
  [UsedImplicitly]
  public class AdminAreaRegistration : AreaRegistration
  {
    public override string AreaName => "Admin";

    public override void RegisterArea(AreaRegistrationContext context)
    {
      context.MapRoute(
        "Admin_default",
        "Admin/{controller}/{action}/{id}",
        new {action = "Index", id = UrlParameter.Optional}
      );
    }
  }
}