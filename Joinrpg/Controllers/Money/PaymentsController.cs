using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using ControllerBase = JoinRpg.Web.Controllers.Common.ControllerBase;

namespace JoinRpg.Web.Controllers.Money
{
    [System.Web.Http.Authorize]
    public class PaymentsController : ControllerBase
    {

        private readonly IPaymentsService _payments;

        public PaymentsController(
            IUserRepository userRepository,
            IPaymentsService payments)
            : base(userRepository)
        {
            _payments = payments;
        }

        [HttpPost]
        [ActionName("claim-payment")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ClaimPayment(PaymentViewModel data)
        {
            if (!data.AcceptContract)
                throw new ArgumentException("Contract must be accepted for online payment", nameof(data.AcceptContract));

            try
            {
                ClaimPaymentContext paymentContext = await _payments.InitiateClaimPayment(
                    new ClaimPaymentRequest
                    {
                        ProjectId = data.ProjectId,
                        ClaimId = data.ClaimId,
                        CommentText = data.CommentText,
                        PayerId = CurrentUserId,
                        Money = data.Money
                    });

                if (paymentContext.Accepted)
                    return Redirect(paymentContext.RedirectUrl);

                throw new Exception();

                // TODO: Redirect to view with error information
            }
            catch
            {
                // TODO: Notify about error
                return RedirectToAction("Edit", "Claim", new { projectId = data.ProjectId, claimId = data.ClaimId });
            }
        }

        [HttpGet]
        [ActionName("claim-payment-success")]
        public async Task<ActionResult> ClaimPaymentSuccess(int operationId)
        {
            throw new NotImplementedException();
        }
    }
}
