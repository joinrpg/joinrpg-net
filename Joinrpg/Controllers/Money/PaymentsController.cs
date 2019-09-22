using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using ControllerBase = JoinRpg.Web.Controllers.Common.ControllerBase;

namespace JoinRpg.Web.Controllers.Money
{
    [System.Web.Http.Authorize]
    public class PaymentsController : ControllerBase
    {

        public PaymentsController(IUserRepository userRepository)
            : base(userRepository)
        {

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ClaimPayment()
        {
            throw new NotImplementedException();
        }

    }
}
