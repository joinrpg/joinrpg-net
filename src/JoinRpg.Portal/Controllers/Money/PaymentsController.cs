using System.Net;
using JoinRpg.DataModel;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PscbApi;

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
            if (data.Method == PaymentMethodViewModel.FastPaymentsSystem)
            {
                if (!Enum.TryParse<FpsPlatform>(data.Platform, true, out var platform))
                {
                    platform = FpsPlatform.Desktop;
                }

                var paymentContext = await _payments.InitiateFastPaymentsSystemMobilePaymentAsync(
                    new ClaimPaymentRequest
                    {
                        ProjectId = data.ProjectId,
                        ClaimId = data.ClaimId,
                        CommentText = data.CommentText,
                        PayerId = CurrentUserAccessor.UserId,
                        Money = data.Money,
                        Method = (PaymentMethod)data.Method,
                        OperationDate = data.OperationDate,
                    },
                    platform);

                return RedirectToAction("FastPaymentsSystemPayment",
                    new
                    {
                        projectId = paymentContext.ProjectId,
                        claimId = paymentContext.ClaimId,
                        orderId = paymentContext.OperationId,
                        platform,
                    });
            }
            else
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
            logger.LogError(e, "Error while updating payment");
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

    [HttpGet]
    [Authorize]
    public async Task<ActionResult> CheckClaimPayment(int projectId, int claimId, int orderId)
    {
        try
        {
            var result = await _payments.UpdateClaimPaymentAsync(projectId, claimId, orderId);
            return result switch
            {
                FinanceOperationState.Approved => Ok(),
                FinanceOperationState.Proposed => StatusCode((int)HttpStatusCode.Accepted),
                FinanceOperationState.Declined
                    or FinanceOperationState.Invalid
                    or FinanceOperationState.Expired => StatusCode((int)HttpStatusCode.UnprocessableEntity),
                _ => BadRequest(),
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Возникла ошибка при попытке обновить статус платежа {paymentId} заявка {claimId}.", orderId, claimId);
            return StatusCode(500);
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ForceRecurrentPayment(int projectId, int claimId, int recurrentPaymentId)
    {
        FinanceOperation? fo;
        try
        {
            fo = await _payments.PerformRecurrentPaymentAsync(projectId, claimId, recurrentPaymentId, null);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while performing recurrent payment");
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

        if (fo is not null)
        {
            try
            {
                await _payments.UpdateClaimPaymentAsync(projectId, claimId, fo.CommentId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while updating recurrent payment state");
            }
        }

        return RedirectToAction("Edit", "Claim", new { projectId, claimId });
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while canceling recurrent payment");
            return Error(
                new ErrorViewModel
                {
                    Message = $"Ошибка отмены рекуррентного платежа ({recurrentPaymentId})",
                    Description = ex.Message,
                    Data = ex,
                    ReturnLink = GetClaimUrl(projectId, claimId),
                    ReturnText = "Вернуться к заявке"
                });
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> RefundPayment(int projectId, int claimId, int operationId)
    {
        try
        {
            await _payments.RefundAsync(projectId, claimId, operationId);
            return RedirectToAction("Edit", "Claim", new { projectId, claimId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while refunding payment");
            return Error(
                new ErrorViewModel
                {
                    Message = $"Ошибка оформления возврата платежа ({operationId})",
                    Description = ex.Message,
                    Data = ex,
                    ReturnLink = GetClaimUrl(projectId, claimId),
                    ReturnText = "Вернуться к заявке"
                });
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult> FastPaymentsSystemPayment(int projectId, int claimId, int orderId, FpsPlatform? platform)
    {
        try
        {
            var paymentContext =
                await _payments.GetFastPaymentsSystemMobilePaymentContextAsync(
                    projectId,
                    claimId,
                    orderId,
                    platform ?? FpsPlatform.Desktop);

            return View("FastPaymentsSystemPayment", paymentContext);
        }
        catch (Exception e)
        {
            try
            {
                await _payments.UpdateClaimPaymentAsync(projectId, claimId, orderId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating payment {financeOperationId} while handling FPS payment startup", orderId);
            }

            return Error(
                new ErrorViewModel
                {
                    Message = "Ошибка обработки платежа: " + e.Message,
                    ReturnLink = GetClaimUrl(projectId, claimId),
                    ReturnText = "Вернуться к заявке",
                    Data = e,
                });
        }
    }
}
