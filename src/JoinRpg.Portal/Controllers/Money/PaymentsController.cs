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
    private readonly ILogger<PaymentsController> logger;
    private readonly IHostEnvironment hostEnvironment;

    public PaymentsController(
        ICurrentUserAccessor currentUserAccessor,
        IPaymentsService payments,
        ILogger<PaymentsController> logger,
        IHostEnvironment hostEnvironment)
    {
        CurrentUserAccessor = currentUserAccessor;
        _payments = payments;
        this.logger = logger;
        this.hostEnvironment = hostEnvironment;
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

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ClaimRecurrentPayment(StartRecurrentPaymentViewModel data)
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
                    Recurrent = true,
                });

            return View("RedirectToBank", paymentContext);
        }
        catch (Exception e)
        {
            return Error(
                new ErrorViewModel
                {
                    Message = "Ошибка создания подписки: " + e.Message,
                    ReturnLink = GetClaimUrl(data.ProjectId, data.ClaimId),
                    ReturnText = "Вернуться к заявке",
                    Data = e,
                });
        }
    }

    /// <summary>
    /// Handles claim payment request
    /// </summary>
    /// <param name="data">Payment data</param>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ClaimPayment(StartOnlinePaymentViewModel data)
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
            logger.LogInformation("Try to handle payment order {orderId} for {claimId}", orderId, claimId);
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
            logger.LogError(e, "Error during handling payment result Description: {description}, ErrorMessage: {errorMessage}", description, errorMessage);
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

    // What we are doing here?
    // 1. ask bank about status of orderId
    // 2. Update status if required
    // 3. Redirect to claim
    [HttpGet]
    [ActionName(nameof(ClaimPaymentSuccess))]
    public async Task<ActionResult> ClaimPaymentSuccessGet(int projectId, int claimId, string orderId)
    {
        if (hostEnvironment.IsProduction())
        {
            return Unauthorized(); //TODO: Remove this action when Shiko will setup local https for testing
        }
        return await HandleClaimPaymentRedirect(projectId, claimId, orderId, "", "Ошибка обработки успешного платежа");
    }

    [HttpPost]
    [IgnoreAntiforgeryToken] // We don't do anything insecure here, just updating payment status and redirect
    public async Task<ActionResult> ClaimPaymentSuccess(int projectId, int claimId, string orderId)
        => await HandleClaimPaymentRedirect(projectId, claimId, orderId, "", "Ошибка обработки успешного платежа");


    [HttpGet]
    [ActionName(nameof(ClaimPaymentFail))]
    public async Task<ActionResult> ClaimPaymentFailGet(int projectId, int claimId, string orderId, string? description)
    {
        if (hostEnvironment.IsProduction())
        {
            return Unauthorized(); //TODO: Remove this action when Shiko will setup local https for testing
        }
        return await HandleClaimPaymentRedirect(projectId, claimId, orderId, description, "Ошибка обработки неудавшегося платежа");
    }

    [HttpPost]
    [IgnoreAntiforgeryToken] // We don't do anything insecure here, just updating payment status and redirect
    public async Task<ActionResult> ClaimPaymentFail(int projectId, int claimId, string orderId, string? description)
        => await HandleClaimPaymentRedirect(projectId, claimId, orderId, description, "Ошибка обработки неудавшегося платежа");

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

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ForceRecurrentPayment(int projectId, int claimId, int recurrentPaymentId)
    {
        try
        {
            await _payments.PerformRecurrentPaymentAsync(projectId, claimId, recurrentPaymentId, null);
            return RedirectToAction("Edit", "Claim", new { projectId, claimId });
        }
        catch (Exception e)
        {
            return Error(
                new ErrorViewModel
                {
                    Message = $"Ошибка принудительного проведения рекуррентного платежа ({recurrentPaymentId})",
                    Description = e.Message,
                    Data = e,
                    ReturnLink = GetClaimUrl(projectId, claimId),
                    ReturnText = "Вернуться к заявке"
                });
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CancelRecurrentPayment(int projectId, int claimId, int recurrentPaymentId)
    {
        try
        {
            await _payments.CancelRecurrentPaymentAsync(projectId, claimId, recurrentPaymentId);
            return RedirectToAction("Edit", "Claim", new { projectId, claimId });
        }
        catch (Exception e)
        {
            return Error(
                new ErrorViewModel
                {
                    Message = $"Ошибка отмены рекуррентного платежа ({recurrentPaymentId})",
                    Description = e.Message,
                    Data = e,
                    ReturnLink = GetClaimUrl(projectId, claimId),
                    ReturnText = "Вернуться к заявке"
                });
        }
    }
}
