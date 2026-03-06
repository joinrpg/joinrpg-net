using JoinRpg.Portal.Controllers.Common;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

public class MyClaimListController : JoinMvcControllerBase
{
    [HttpGet("/my/claims")]
    public ActionResult My() => RedirectToActionPermanent("Me", "User");
}
