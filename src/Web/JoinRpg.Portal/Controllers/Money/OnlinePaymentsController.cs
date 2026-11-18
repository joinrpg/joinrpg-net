
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Web.Controllers.Money;

public class OnlinePaymentsController : Controller
{
    [HttpGet]
    public ActionResult Index() => View();

    [HttpGet]
    [ActionName("user-agreement")]
    public ActionResult UserAgreement()
        => Redirect(Documents.UserContract);

    [HttpGet]
    [ActionName("organizer-contract")]
    public ActionResult OrganizerContract()
        => Redirect(Documents.OrganizerContract);
}
