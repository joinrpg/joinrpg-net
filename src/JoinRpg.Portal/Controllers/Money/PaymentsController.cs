using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.Money;

public class PaymentsController : Common.ControllerBase
{
    private ICurrentUserAccessor CurrentUserAccessor { get; }
    private readonly IPaymentsService _payments;

    public PaymentsController(
        ICurrentUserAccessor currentUserAccessor,
        IPaymentsService payments)
    {
        CurrentUserAccessor = currentUserAccessor;
        _payments = payments;
    }

    private string? GetClaimUrl(int projectId, int claimId)
        => Url.Action("Edit", "Claim", new { projectId, claimId });

    /// <summary>
    /// Returns payment error view
    /// </summary>
    public ActionResult Error(ErrorViewModel model)
    {
        model.Debug = model.Debug || CurrentUserAccessor.IsAdmin;
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
                    PayerId = CurrentUserAccessor.UserId,
                    Money = data.Money,
                    Method = (PaymentMethod)data.Method,
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
                    ReturnText = "Вернуться к заявке",
                    Data = e,
                });
        }
    }

    private async Task<ActionResult> HandleClaimPaymentRedirect(int projectId, int claimId, string orderId, string? description, string errorMessage)
    {
        var financeOperationId = 0;
        try
        {
            if (int.TryParse(orderId, out financeOperationId))
            {
                await _payments.UpdateClaimPaymentAsync(projectId, claimId, financeOperationId);
            }
            else
            {
                await _payments.UpdateLastClaimPaymentAsync(projectId, claimId);
            }

            // TODO: In case of invalid payment redirect to special page
            return RedirectToAction("Edit", "Claim", new { projectId, claimId });
        }
        catch (Exception e)
        {
            string foText = financeOperationId > 0
                ? financeOperationId.ToString()
                : "unknown finance operation";
            return Error(
                new ErrorViewModel
                {
                    Message = $"{errorMessage} {foText}",
                    Description = e.Message,
                    Data = e,
                    ReturnLink = GetClaimUrl(projectId, claimId),
                    ReturnText = "Вернуться к заявке",
                });
        }
    }

    //TODO: why we are losing cookies here? It's ok, because we don't do anything insecure here, but still...
    // What we are doing here?
    // 1. ask bank about status of orderId
    // 2. Update status if required
    // 3. Redirect to claim
    [HttpGet]
    [AllowAnonymous]
    [ActionName(nameof(ClaimPaymentSuccess))]
    public async Task<ActionResult> ClaimPaymentSuccessGet(int projectId, int claimId, string orderId)
        => await HandleClaimPaymentRedirect(projectId, claimId, orderId, "",
            "Ошибка обработки успешного платежа");


    [HttpPost]
    [AllowAnonymous] //TODO see above
    public async Task<ActionResult> ClaimPaymentSuccess(int projectId, int claimId, string orderId)
        => await HandleClaimPaymentRedirect(projectId, claimId, orderId, "",
            "Ошибка обработки успешного платежа");


    [HttpGet]
    [AllowAnonymous] //TODO see above
    [ActionName(nameof(ClaimPaymentFail))]
    public async Task<ActionResult> ClaimPaymentFailGet(int projectId, int claimId, string orderId,
        string? description)
        => await HandleClaimPaymentRedirect(projectId, claimId, orderId, description,
            "Ошибка обработки неудавшегося платежа");

    [HttpPost]
    [AllowAnonymous] //TODO see above
    public async Task<ActionResult> ClaimPaymentFail(int projectId, int claimId, string orderId,
        string? description)
        => await HandleClaimPaymentRedirect(projectId, claimId, orderId, description,
            "Ошибка обработки неудавшегося платежа");

    [HttpGet]
    [Authorize]
    public async Task<ActionResult> UpdateClaimPayment(int projectId, int claimId, int orderId)
    {
        try
        {
            await _payments.UpdateClaimPaymentAsync(projectId, claimId, orderId);
            return RedirectToAction("Edit", "Claim", new { projectId, claimId });
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
