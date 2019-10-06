using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using PscbApi;

namespace JoinRpg.Web.Controllers.Money
{
    public class PaymentsController : Common.ControllerBase
    {
        private readonly IPaymentsService _payments;

        public PaymentsController(
            IUserRepository userRepository,
            IPaymentsService payments)
            : base(userRepository)
        {
            _payments = payments;
        }

        private string GetClaimUrl(int projectId, int claimId)
            => Url.Action("Edit", "Claim", new {projectId, claimId});

        /// <summary>
        /// Returns payment error view
        /// </summary>
        public ActionResult Error(ErrorViewModel model)
        {
            model.Title = string.IsNullOrWhiteSpace(model.Title)
                ? "Ошибка онлайн-оплаты"
                : model.Title.Trim();
            return View("Error", model);
        }

        /// <summary>
        /// Handles claim payment request
        /// </summary>
        /// <param name="data">Payment data</param>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ClaimPayment(PaymentViewModel data)
        {
            // Checking contract
            if (!data.AcceptContract)
            {
                return Error(
                    new ErrorViewModel
                    {
                        Message = "Необходимо принять оферту",
                        ReturnLink = GetClaimUrl(data.ProjectId, data.ClaimId),
                        ReturnText = "Вернуться к заявке"
                    });
            }

            try
            {
                ClaimPaymentContext paymentContext = await _payments.InitiateClaimPaymentAsync(
                    new ClaimPaymentRequest
                    {
                        ProjectId = data.ProjectId,
                        ClaimId = data.ClaimId,
                        CommentText = data.CommentText,
                        PayerId = CurrentUserId,
                        Money = data.Money,
                        OperationDate = data.OperationDate,
                    });

                return View("RedirectToBank", paymentContext);
            }
            catch (Exception e)
            {
                return Error(
                    new ErrorViewModel
                    {
                        Message = "Ошибка создания платежа: " + e.Message,
                        ReturnLink = GetClaimUrl(data.ProjectId, data.ClaimId),
                        ReturnText = "Вернуться к заявке"
                    });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> ClaimPaymentSuccess(
            int projectId,
            int claimId,
            string orderId,
            [CanBeNull]
            string description)
        {
            if (int.TryParse(orderId, out var financeOperationId))
            {
                var resultContext = new ClaimPaymentResultContext
                {
                    ProjectId = projectId,
                    ClaimId = claimId,
                    OrderId = financeOperationId,
                    Succeeded = true,
                    BankResponse = ProtocolHelper.ParseDescriptionString(description)
                };
                try
                {
                    await _payments.SetClaimPaymentResultAsync(resultContext);
                    return RedirectToAction("Edit", "Claim", new {projectId, claimId});
                }
                catch (Exception e)
                {
                    return Error(
                        new ErrorViewModel
                        {
                            Message = $"Ошибка обработки успешного платежа {orderId}",
                            Description = e.Message,
                            Data = e,
                            ReturnLink = GetClaimUrl(projectId, claimId),
                            ReturnText = "Вернуться к заявке",
                        });
                }
            }

            return Error(
                new ErrorViewModel
                {
                    Message = $"Неверный идентификатор платежа: {orderId}",
                    ReturnLink = GetClaimUrl(projectId, claimId),
                    ReturnText = "Вернуться к заявке",
                });
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> ClaimPaymentFail(
            int projectId,
            int claimId,
            string orderId,
            [CanBeNull]
            string description)
        {
            if (int.TryParse(orderId, out var financeOperationId))
            {
                // Creating result context
                var resultContext = new ClaimPaymentResultContext
                {
                    ProjectId = projectId,
                    ClaimId = claimId,
                    OrderId = financeOperationId,
                    Succeeded = false,
                    BankResponse = ProtocolHelper.ParseDescriptionString(description)
                };
                // Creating message
                var message = "";
                if (!string.IsNullOrWhiteSpace(resultContext.BankResponse.Description))
                    message += ": " + resultContext.BankResponse.Description;
                string codes = string.Join(
                    ", ",
                    new string[]
                        {
                            resultContext.BankResponse.PaymentCode?.ToString(),
                            resultContext.BankResponse.ProcessingCode?.ToString()
                        }.Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToArray());
                if (!string.IsNullOrWhiteSpace(codes))
                    message += $" ({codes})";

                // Processing result
                try
                {
                    await _payments.SetClaimPaymentResultAsync(resultContext);

                    if (IsCurrentUserAdmin())
                        return Error(
                            new ErrorViewModel
                            {
                                Message = "Ошибка выполнения платежа" + message,
                                ReturnLink = GetClaimUrl(projectId, claimId),
                                ReturnText = "Вернуться к заявке"
                            });
                    return RedirectToAction("Edit", "Claim", new {projectId, claimId});
                }
                catch (Exception e)
                {
                    return Error(
                        new ErrorViewModel
                        {
                            Message =
                                $"Ошибка обработки неудавшегося платежа ({orderId}): {message}",
                            Description = e.Message,
                            Data = e,
                            ReturnLink = GetClaimUrl(projectId, claimId),
                            ReturnText = "Вернуться к заявке"
                        });
                }
            }

            return Error(
                new ErrorViewModel
                {
                    Message = $"Неверный идентификатор платежа: {orderId}",
                    ReturnLink = GetClaimUrl(projectId, claimId),
                    ReturnText = "Вернуться к заявке",
                });
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> UpdateClaimPayment(int projectId, int claimId, int orderId)
        {
            try
            {
                await _payments.UpdateClaimPaymentAsync(projectId, claimId, orderId);
                return RedirectToAction("Edit", "Claim", new {projectId, claimId});
            }
            catch (Exception e)
            {
                return Error(
                    new ErrorViewModel
                    {
                        Message = $"Ошибка обновления статуса платежа ({orderId})",
                        Description = e.Message,
                        Data = e,
                        ReturnLink = GetClaimUrl(projectId, claimId),
                        ReturnText = "Вернуться к заявке"
                    });
            }
        }
    }
}
