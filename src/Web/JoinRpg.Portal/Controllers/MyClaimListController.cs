using JoinRpg.Portal.Controllers.Common;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

public class MyClaimListController : JoinMvcControllerBase
{
    [HttpGet("/my/claims")]
    public ActionResult My() => RedirectToActionPermanent("Me", "User");

    /// <summary>
    /// Старый адрес списка своих заявок. Остался в закладках и во внешних ссылках.
    /// </summary>
    [HttpGet("/claimlist/my")]
    public ActionResult LegacyMy() => RedirectToActionPermanent(nameof(My));
}
