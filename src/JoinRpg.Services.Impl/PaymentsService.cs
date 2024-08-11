using System.Data.Entity;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Logging;
using PscbApi;
using PscbApi.Models;

namespace JoinRpg.Services.Impl;

public static class FinanceOperationExtensions
{
    /// <summary>
    /// Creates the order id that is used to identify our payments on the bank side
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetOrderId(this FinanceOperation fo) => GetOrderId(fo.CommentId);

    /// <summary>
    /// Creates the order id that is used to identify our payments on the bank side
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetOrderId(this Comment comment) => GetOrderId(comment.CommentId);

    /// <summary>
    /// Creates the order id that is used to identify our payments on the bank side
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetOrderId(int id) => id.ToString().PadLeft(10, '0');
}

/// <inheritdoc cref="IPaymentsService" />
public class PaymentsService(
    IUnitOfWork unitOfWork,
    IUriService uriService,
    IBankSecretsProvider bankSecrets,
    ICurrentUserAccessor currentUserAccessor,
    Lazy<IEmailService> emailService,
    ILogger<PaymentsService> logger,
    IProjectMetadataRepository projectMetadataRepository,
    IHttpClientFactory clientFactory) : DbServiceImplBase(unitOfWork, currentUserAccessor), IPaymentsService
{
    private ApiConfiguration GetApiConfiguration(int projectId, int claimId)
    {
        return new ApiConfiguration
        {
            Debug = bankSecrets.Debug,
            ApiEndpoint = bankSecrets.ApiEndpoint,
            ApiDebugEndpoint = bankSecrets.ApiDebugEndpoint,
            MerchantId = bankSecrets.MerchantId,
            ApiKey = bankSecrets.ApiKey,
            ApiDebugKey = bankSecrets.ApiDebugKey,
            DefaultSuccessUrl = uriService.Get(new PaymentSuccessUrl(projectId, claimId)),
            DefaultFailUrl = uriService.Get(new PaymentFailUrl(projectId, claimId)),
        };
    }

    private BankApi GetApi(int projectId, int claimId)
        => new(clientFactory, GetApiConfiguration(projectId, claimId));

    private async Task<Claim> GetClaimAsync(int projectId, int claimId)
    {
        var claim = await UnitOfWork.GetClaimsRepository()
            .GetClaim(projectId, claimId);
        return claim ?? throw new JoinRpgEntityNotFoundException(claimId, nameof(Claim));
    }

    // TODO: We have to reimagine how we get payment purpose
    private (string GoodName, string Details) GetPurpose(bool recurrent, string projectName)
    {
        var goodName = recurrent
            ? "Сервисы JoinRpg"
            : projectName;
        var details = recurrent
            ? $"Подписка пользователя на {goodName}"
            : $"Билет (организационный взнос) участника на {goodName}";
        return (goodName, details);
    }

    /// <inheritdoc />
    public async Task<ClaimPaymentContext> InitiateClaimPaymentAsync(ClaimPaymentRequest request)
    {
        // Loading claim
        var claim = await GetClaimAsync(request.ProjectId, request.ClaimId);

        // Checking access rights
        if (claim.PlayerUserId != CurrentUserId)
        {
            throw new NoAccessToProjectException(claim.Project, CurrentUserId);
        }

        var onlinePaymentType = request.Recurrent
            ? claim.Project.ActivePaymentTypes.SingleOrDefault(pt => pt.TypeKind == PaymentTypeKind.OnlineSubscription)
            : claim.Project.ActivePaymentTypes.SingleOrDefault(pt => pt.TypeKind == PaymentTypeKind.Online);

        if (onlinePaymentType is null)
        {
            throw new OnlinePaymentsNotAvailableException(claim.Project);
        }

        if (request.Recurrent && request.Method != PaymentMethod.FastPaymentsSystem)
        {
            throw new PaymentMethodNotAllowedForRecurrentPaymentsException(claim.Project, request.Method);
        }

        if (request.Money <= 0)
        {
            throw new PaymentException(claim.Project, $"Money amount must be positive integer");
        }

        User user = await GetCurrentUser();

        var purpose = GetPurpose(request.Recurrent, claim.Project.ProjectName);

        var message = new PaymentMessage
        {
            RecurrentPayment = request.Recurrent,
            Amount = request.Money,
            Details = purpose.Details,
            CustomerAccount = CurrentUserId.ToString(),
            CustomerEmail = user.Email,
            CustomerPhone = user.Extra?.PhoneNumber,
            CustomerComment = request.CommentText ?? purpose.Details,
            PaymentMethod = request.Method switch
            {
                PaymentMethod.BankCard => PscbPaymentMethod.BankCards,
                PaymentMethod.FastPaymentsSystem => PscbPaymentMethod.FastPaymentsSystem,
                _ => throw new NotSupportedException($"Payment method {request.Method} is not supported"),
            },
            SuccessUrl = uriService.Get(new PaymentSuccessUrl(request.ProjectId, request.ClaimId)),
            FailUrl = uriService.Get(new PaymentFailUrl(request.ProjectId, request.ClaimId)),
            Data = new PaymentMessageData
            {
                FastPaymentsSystemSubscriptionPurpose = request.Recurrent ? purpose.Details : null,
                // FastPaymentsSystemRedirectUrl = request.Method == PaymentMethod.FastPaymentsSystem
                //     ? uriService.Get(new PaymentSuccessUrl(request.ProjectId, request.ClaimId))
                //     : null,
                Receipt = new Receipt
                {
                    CompanyEmail = User.OnlinePaymentVirtualUser,
                    TaxSystem = TaxSystemType.SimplifiedIncomeOutcome,
                    Items = new List<ReceiptItem>
                    {
                        new ReceiptItem
                        {
                            ObjectType = PaymentObjectType.Service,
                            PaymentType = ItemPaymentType.FullPayment,
                            Price = request.Money,
                            Quantity = 1,
                            TotalPrice = request.Money,
                            VatType = VatSystemType.None,
                            Name = purpose.Details,
                        }
                    }
                }
            }
        };

        // Creating request to bank
        PaymentRequestDescriptor result = await GetApi(request.ProjectId, request.ClaimId)
            .BuildPaymentRequestAsync(
                message,
                async () => (await AddPaymentCommentAsync(claim, onlinePaymentType, request)).GetOrderId()
            );

        return new ClaimPaymentContext
        {
            Accepted = true,
            RequestDescriptor = result
        };
    }

    private async Task<Comment> AddPaymentCommentAsync(
        Claim claim,
        PaymentType paymentType,
        ClaimPaymentRequest request)
    {
        if (request is { Recurrent: true, Refund: true })
        {
            throw new ArgumentException($"{nameof(ClaimPaymentRequest.Refund)} and {nameof(ClaimPaymentRequest.Recurrent)} flags are not compatible", nameof(request));
        }

        if (request is { Refund: true, FinanceOperationToRefundId: null })
        {
            throw new ArgumentException($"{nameof(ClaimPaymentRequest.FinanceOperationToRefundId)} is required when {nameof(ClaimPaymentRequest.Refund)} is true", nameof(request));
        }

        RecurrentPayment? recurrentPayment = null;
        if (request.Recurrent)
        {
            recurrentPayment = new RecurrentPayment
            {
                ClaimId = claim.ClaimId,
                ProjectId = claim.ProjectId,
                Status = RecurrentPaymentStatus.Created,
                CreateDate = Now,
                PaymentAmount = request.Money,
                PaymentTypeId = paymentType.PaymentTypeId,
            };
            UnitOfWork.GetDbSet<RecurrentPayment>().Add(recurrentPayment);
            await UnitOfWork.SaveChangesAsync();
        }

        Comment comment = CommentHelper.CreateCommentForClaim(
            claim,
            CurrentUserId,
            Now,
            // Do not remove null-coalescing here!
            // Payment comment is not necessary, but it must not be null to create comment.
            request.CommentText ?? "",
            true,
            null);
        comment.Finance = new FinanceOperation
        {
            OperationType = request.Refund ? FinanceOperationType.Refund : FinanceOperationType.Online,
            RefundedOperationId = request.Refund ? request.FinanceOperationToRefundId : null,
            PaymentTypeId = paymentType.PaymentTypeId,
            MoneyAmount = request.Refund ? -request.Money : request.Money,
            OperationDate = request.OperationDate,
            ProjectId = request.ProjectId,
            ClaimId = request.ClaimId,
            Created = Now,
            Changed = Now,
            State = FinanceOperationState.Proposed,
            RecurrentPaymentId = recurrentPayment?.RecurrentPaymentId ?? request.FromRecurrentPaymentId,
        };
        _ = UnitOfWork.GetDbSet<Comment>().Add(comment);
        await UnitOfWork.SaveChangesAsync();

        return comment;
    }

    private async Task<FinanceOperation> LoadFinanceOperationAsync(int projectId, int claimId, int operationId)
    {
        // Loading finance operation
        FinanceOperation fo = await UnitOfWork.GetDbSet<FinanceOperation>()
            .Include(e => e.Claim)
            .Include(e => e.RecurrentPayment)
            .Include(e => e.PaymentType)
            .FirstOrDefaultAsync(e => e.CommentId == operationId);

        if (fo == null)
        {
            throw new JoinRpgEntityNotFoundException(operationId, nameof(FinanceOperation));
        }

        if (fo.ClaimId != claimId)
        {
            throw new JoinRpgEntityNotFoundException(claimId, nameof(Claim));
        }

        if (fo.ProjectId != projectId)
        {
            throw new JoinRpgEntityNotFoundException(projectId, nameof(Project));
        }

        if (fo.PaymentType?.TypeKind.IsOnline() != true || fo.OperationType is not (FinanceOperationType.Online or FinanceOperationType.Refund))
        {
            throw new PaymentException(fo.Project, "Finance operation is not online payment");
        }

        return fo;
    }

    private async Task<FinanceOperation?> LoadLastUnapprovedFinanceOperationAsync(int projectId, int claimId)
    {
        const int pageSize = 5;

        // Грязный чит
        var skip = 0;
        while (true)
        {
            // Берем по пять штук
            var operations = await UnitOfWork.GetDbSet<FinanceOperation>()
                .Include(e => e.RecurrentPayment)
                .Include(e => e.PaymentType)
                .Where(e => e.ProjectId == projectId
                            && e.ClaimId == claimId
                            && e.OperationType == FinanceOperationType.Online
                            && e.State == FinanceOperationState.Proposed)
                .OrderByDescending(e => e.Created)
                .Skip(skip)
                .Take(pageSize)
                .ToArrayAsync();
            if (operations.Length == 0)
            {
                return null;
            }

            // Ищем первую подходящую
            var result = operations.FirstOrDefault(e => e.PaymentType?.TypeKind.IsOnline() is true);

            // Если нашли, или загружено меньше страницы — выходим
            if (result is not null || operations.Length < pageSize)
            {
                return result;
            }

            skip += pageSize;
        }
    }

    private void UpdateFinanceOperationStatus(FinanceOperation fo, RefundData? refundData)
    {
        if (fo.Approved)
        {
            return;
        }

        if (refundData is null)
        {
            fo.State = FinanceOperationState.Invalid;
        }
        else if (refundData.Status == RefundStatus.Error)
        {
            fo.State = FinanceOperationState.Declined;
        }
        else if (refundData.Status == RefundStatus.Completed)
        {
            fo.State = FinanceOperationState.Approved;
        }

        fo.Changed = Now;
    }

    private void UpdateFinanceOperationStatus(FinanceOperation fo, PaymentData paymentData)
    {
        switch (paymentData.Status)
        {
            // Do nothing
            case PaymentStatus.New:
            case PaymentStatus.AwaitingForPayment:
            case PaymentStatus.Refunded:
            case PaymentStatus.Hold:
            case PaymentStatus.Undefined:
                break;

            // All ok
            case PaymentStatus.Paid:
                fo.State = FinanceOperationState.Approved;
                fo.Changed = Now;
                break;

            // User didn't do anything on payment page
            case PaymentStatus.Expired:
                fo.State = FinanceOperationState.Invalid;
                fo.Changed = Now;
                break;

            // Something went wrong
            case PaymentStatus.Cancelled:
            case PaymentStatus.Error: // TODO: Probably have to store last error within finance op?
            case PaymentStatus.Rejected:
                fo.State = FinanceOperationState.Declined;
                fo.Changed = Now;
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task UpdateClaimPaymentAsync(FinanceOperation fo)
    {
        if (fo.State != FinanceOperationState.Proposed)
        {
            return;
        }

        logger.LogInformation("Updating online payment {financeOperationId} for claim {claimId} to project {projectId}", fo.CommentId, fo.ClaimId, fo.ProjectId);

        var orderIdStr = fo.RefundOperation
            ? FinanceOperationExtensions.GetOrderId(fo.RefundedOperationId.GetValueOrDefault())
            : fo.GetOrderId();

        // Preparing recurrent payment object
        RecurrentPayment? recurrentPayment = fo.RecurrentPayment;
        if (recurrentPayment is null && fo.RecurrentPaymentId.HasValue)
        {
            recurrentPayment = await UnitOfWork.GetDbSet<RecurrentPayment>().FindAsync(fo.RecurrentPaymentId.Value);
        }

        // If recurrent payment is not in created state or this finance operation is not parent finance operation,
        // we should not do anything with it
        if (recurrentPayment?.Status != RecurrentPaymentStatus.Created
            || !string.Equals(recurrentPayment.BankParentPayment, orderIdStr, StringComparison.OrdinalIgnoreCase))
        {
            recurrentPayment = null;
        }

        // Asking bank
        var api = GetApi(fo.ProjectId, fo.ClaimId);
        PaymentInfo paymentInfo = await api.GetPaymentInfoAsync(orderIdStr);

        Claim? claim = null;
        PaymentNotification paymentNotification = PaymentNotification.None;

        // Updating status
        if (paymentInfo.Status == PaymentInfoQueryStatus.Success)
        {
            // Unknown payment means our object is detached somehow from bank object
            if (paymentInfo.ErrorCode == ApiErrorCode.UnknownPayment)
            {
                logger.LogError("Online payment {financeOperationId} for claim {claimId} to project {projectId} has failed", fo.CommentId, fo.ClaimId, fo.ProjectId);
                fo.State = FinanceOperationState.Declined;
                fo.Changed = Now;
            }
            // We have refund operation that was successfully loaded
            else if (fo.RefundOperation && paymentInfo.ErrorCode is null)
            {
                // Trying to get specific refund by its id
                var refund = paymentInfo.Payment.Refunds?.FirstOrDefault(rf => string.Equals(rf.Id, fo.BankRefundToken, StringComparison.OrdinalIgnoreCase));

                // Updating operation status. If no refund -- no problem, it makes operation invalid
                UpdateFinanceOperationStatus(fo, refund);

                claim = await GetClaimAsync(fo.ProjectId, fo.ClaimId);

                if (fo.Approved)
                {
                    var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(fo.ProjectId));
                    claim.UpdateClaimFeeIfRequired(Now, projectInfo);
                }

                paymentNotification = PaymentNotification.Refund;
            }
            // We have regular operation that was successfully loaded
            else if (!fo.RefundOperation && paymentInfo.ErrorCode is null)
            {
                UpdateFinanceOperationStatus(fo, paymentInfo.Payment);
                if (fo.State == FinanceOperationState.Approved)
                {
                    logger.LogInformation("Online payment {financeOperationId} for claim {claimId} to project {projectId} has been successfully performed", fo.CommentId, fo.ClaimId, fo.ProjectId);

                    claim = await GetClaimAsync(fo.ProjectId, fo.ClaimId);
                    var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(fo.ProjectId));

                    if (recurrentPayment is not null)
                    {
                        recurrentPayment.BankRecurrencyToken = paymentInfo.Payment.RecurrentPaymentToken;
                        if (string.IsNullOrEmpty(recurrentPayment.BankRecurrencyToken))
                        {
                            recurrentPayment.BankRecurrencyToken = null;
                        }
                        else
                        {
                            recurrentPayment.BankParentPayment = orderIdStr;
                            recurrentPayment.Status = RecurrentPaymentStatus.Initialization;
                        }
                    }

                    claim.UpdateClaimFeeIfRequired(Now, projectInfo);

                    paymentNotification = fo.RecurrentPaymentId.HasValue
                        ? PaymentNotification.RecurrentCharge
                        : PaymentNotification.Payment;
                }
            }
        }
        else if (paymentInfo.ErrorCode == ApiErrorCode.UnknownPayment)
        {
            fo.State = FinanceOperationState.Invalid;
            fo.Changed = Now;
        }

        if (paymentInfo.ErrorCode is not null)
        {
            logger.LogError("Error updating payment {financeOperationId} with bank code {bankErrorCode} and details:\n{bankError}", fo.CommentId, paymentInfo.ErrorCode, paymentInfo.ErrorDescription);
        }

        // When recurrent payment is not null and its state is still created, it means we can mark it failed
        if (recurrentPayment?.Status == RecurrentPaymentStatus.Created)
        {
            recurrentPayment.CloseDate = Now;
            recurrentPayment.Status = RecurrentPaymentStatus.Failed;
        }

        if (recurrentPayment?.Status == RecurrentPaymentStatus.Failed)
        {
            logger.LogError("Recurrent payment {recurrentPaymentId} setup for claim {claimId} to project {projectId} has failed", recurrentPayment.RecurrentPaymentId, fo.ClaimId, fo.ProjectId);
        }

        // Saving if status was updated
        if (fo.State != FinanceOperationState.Proposed)
        {
            await UnitOfWork.SaveChangesAsync();
        }

        // Let's continue with recurrent payments initialization when needed
        if (recurrentPayment?.Status is RecurrentPaymentStatus.Initialization)
        {
            logger.LogInformation("Configuring recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId}", recurrentPayment.RecurrentPaymentId, fo.ClaimId, fo.ProjectId);

            var purpose = GetPurpose(true, string.Empty);
            var result = await api.SetupFastPaymentSystemRecurrentPayments(
                orderIdStr,
                recurrentPayment.BankRecurrencyToken!,
                fo.MoneyAmount,
                purpose.Details);

            if (result.Status == PaymentInfoQueryStatus.Success)
            {
                logger.LogInformation("Recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId} has been configured", recurrentPayment.RecurrentPaymentId, fo.ClaimId, fo.ProjectId);
                recurrentPayment.Status = RecurrentPaymentStatus.Active;
                recurrentPayment.BankAdditional = result.FastPaymentSystemRecurrencyId;

                paymentNotification = PaymentNotification.RecurrentSetupAndCharge;
            }
            else
            {
                logger.LogError("Recurrent payment {recurrentPaymentId} setup for claim {claimId} to project {projectId} has failed", recurrentPayment.RecurrentPaymentId, fo.ClaimId, fo.ProjectId);
                recurrentPayment.Status = RecurrentPaymentStatus.Failed;
                recurrentPayment.CloseDate = Now;

                paymentNotification = fo.Approved ? PaymentNotification.Payment : PaymentNotification.None;
            }

            await UnitOfWork.SaveChangesAsync();
        }

        // Sending payment notification when needed
        if (paymentNotification != PaymentNotification.None)
        {
            Debug.Assert(claim is not null);
            await SendPaymentNotification(claim, fo.MoneyAmount, paymentNotification);
        }
    }

    private enum PaymentNotification
    {
        None,
        Payment,
        RecurrentSetup,
        RecurrentSetupAndCharge,
        RecurrentCharge,
        Refund,
    }

    private async Task SendPaymentNotification(Claim claim, int sum, PaymentNotification notification)
    {
        if (notification == PaymentNotification.None)
        {
            return;
        }

        try
        {
            var sb = new StringBuilder();
            // TODO: Localize
            switch (notification)
            {
                case PaymentNotification.Payment:
                    sb.Append($"Онлайн-оплата на сумму {sum:F2}₽ подтверждена.");
                    break;
                case PaymentNotification.RecurrentSetup:
                    sb.AppendLine($"Оформлена ежемесячная подписка на сумму {sum:F2}₽.");
                    sb.AppendLine("Списания будут проводиться автоматически.");
                    sb.AppendLine();
                    sb.Append($"Чтобы отказаться от подписки, перейдите в свою заявку: {uriService.Get(claim)}");
                    break;
                case PaymentNotification.RecurrentSetupAndCharge:
                    sb.AppendLine($"Оформлена ежемесячная подписка на сумму {sum:F2}₽.");
                    sb.AppendLine($"Первичное списание средств по подписке на сумму {sum:F2}₽ подтверждено.");
                    sb.AppendLine();
                    sb.Append($"Чтобы отказаться от подписки, перейдите в свою заявку: {uriService.Get(claim)}");
                    break;
                case PaymentNotification.RecurrentCharge:
                    sb.Append($"Списание средств по подписке на сумму {sum:F2}₽ подтверждено.");
                    sb.AppendLine();
                    sb.Append($"Чтобы отказаться от подписки, перейдите в свою заявку: {uriService.Get(claim)}");
                    break;
                case PaymentNotification.Refund:
                    sb.AppendLine($"Проведен возврат на сумму {sum:F2}₽ на использованное средство платежа.");
                    sb.AppendLine();
                    sb.Append("Срок фактического зачисления средств зависит от вашего банка, но как правило не превышает 3 банковских дней.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(notification), notification, "Unknown payment notification");
            }

            var subscriptions = claim.GetSubscriptions(p => p.MoneyOperation, [], mastersOnly: true).ToList();
            var email = new FinanceOperationEmail()
            {
                Claim = claim,
                ProjectName = claim.Project.ProjectName,
                Initiator = claim.Player,
                InitiatorType = ParcipantType.Player,
                Recipients = subscriptions,
                Text = new MarkdownString(sb.ToString()),
                CommentExtraAction = null,
            };

            await emailService.Value.Email(email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while sending payment notification of claim {claimId}", claim.ClaimId);
        }
    }

    /// <inheritdoc />
    public async Task UpdateClaimPaymentAsync(int projectId, int claimId, int orderId)
        => await UpdateClaimPaymentAsync(await LoadFinanceOperationAsync(projectId, claimId, orderId));

    /// <inheritdoc />
    public async Task UpdateLastClaimPaymentAsync(int projectId, int claimId)
    {
        var fo = await LoadLastUnapprovedFinanceOperationAsync(projectId, claimId);
        if (fo is not null)
        {
            await UpdateClaimPaymentAsync(fo);
        }
    }

    /// <inheritdoc />
    public async Task<bool?> CancelRecurrentPaymentAsync(int projectId, int claimId, int recurrentPaymentId)
    {
        logger.LogInformation("Trying to cancel recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId}", recurrentPaymentId, claimId, projectId);

        var recurrentPayment = await UnitOfWork.GetDbSet<RecurrentPayment>()
            .Where(rp => rp.ClaimId == claimId && rp.ProjectId == projectId && rp.RecurrentPaymentId == recurrentPaymentId)
            .FirstOrDefaultAsync();

        if (recurrentPayment is null)
        {
            logger.LogError("There is no recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId}", recurrentPaymentId, claimId, projectId);
            throw new JoinRpgEntityNotFoundException(recurrentPaymentId, nameof(RecurrentPayment));
        }

        if (!recurrentPayment.Claim.HasPlayerAccesToClaim(CurrentUserId)
              && !recurrentPayment.HasMasterAccess(CurrentUserId, a => a.CanManageMoney))
        {
            throw new JoinRpgInvalidUserException();
        }

        if (recurrentPayment.Status is not (RecurrentPaymentStatus.Created or RecurrentPaymentStatus.Active or RecurrentPaymentStatus.Cancelling))
        {
            logger.LogError("It is not possible to cancel recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId} because its state {recurrentPaymentState} is not appropriate", recurrentPayment.RecurrentPaymentId, claimId, projectId, recurrentPayment.Status);
            return null; // TODO: Do we need to throw something here?
        }

        recurrentPayment.Status = recurrentPayment.Status == RecurrentPaymentStatus.Created
            ? RecurrentPaymentStatus.Cancelled
            : RecurrentPaymentStatus.Cancelling;
        await UnitOfWork.SaveChangesAsync();

        if (recurrentPayment.Status == RecurrentPaymentStatus.Cancelled)
        {
            logger.LogInformation("Recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId} has been successfully cancelled", recurrentPaymentId, claimId, projectId);
            return true;
        }

        var api = GetApi(projectId, claimId);
        var result = await api.CancelFastPaymentSystemRecurrentPayments(recurrentPayment.BankParentPayment, recurrentPayment.BankRecurrencyToken);
        if (result.Status == PaymentInfoQueryStatus.Success)
        {
            recurrentPayment.Status = RecurrentPaymentStatus.Cancelled;
            await UnitOfWork.SaveChangesAsync();

            logger.LogInformation("Recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId} has been successfully cancelled", recurrentPaymentId, claimId, projectId);
            return true;
        }

        logger.LogError("Failed to cancel recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId} because {bankError}", recurrentPayment.RecurrentPaymentId, claimId, projectId, result.ErrorDescription);
        return false;
    }

    /// <inheritdoc />
    public async Task<FinanceOperation?> PerformRecurrentPaymentAsync(int projectId, int claimId, int recurrentPaymentId, int? amount, bool internalCall = false)
    {
        logger.LogInformation("Trying to perform recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId}", recurrentPaymentId, claimId, projectId);

        var recurrentPayment = await UnitOfWork.GetDbSet<RecurrentPayment>()
            .Include(rp => rp.Claim)
            .Include(rp => rp.Project)
            .Include(rp => rp.PaymentType)
            .Where(rp => rp.ClaimId == claimId && rp.ProjectId == projectId && rp.RecurrentPaymentId == recurrentPaymentId)
            .FirstOrDefaultAsync();

        if (recurrentPayment is null)
        {
            logger.LogError("There is no recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId}", recurrentPaymentId, claimId, projectId);
            throw new JoinRpgEntityNotFoundException(recurrentPaymentId, nameof(RecurrentPayment));
        }

        if (!internalCall)
        {
            if (!recurrentPayment.Claim.HasAccess(CurrentUserId, e => e.CanManageMoney))
            {
                throw new JoinRpgInvalidUserException();
            }
        }

        if (recurrentPayment.Status is not RecurrentPaymentStatus.Active)
        {
            logger.LogError("Recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId} state {recurrentPaymentState} is not active", recurrentPayment.RecurrentPaymentId, claimId, projectId, recurrentPayment.Status);
            return null; // TODO: Do we need to throw something here?
        }

        var purpose = GetPurpose(true, string.Empty);

        var receipt = new Receipt
        {
            CompanyEmail = User.OnlinePaymentVirtualUser,
            TaxSystem = TaxSystemType.SimplifiedIncomeOutcome,
            Items = new List<ReceiptItem>
            {
                new ReceiptItem
                {
                    ObjectType = PaymentObjectType.Service,
                    PaymentType = ItemPaymentType.FullPayment,
                    Price = amount ?? recurrentPayment.PaymentAmount,
                    Quantity = 1,
                    TotalPrice = amount ?? recurrentPayment.PaymentAmount,
                    VatType = VatSystemType.None,
                    Name = purpose.Details,
                }
            }
        };

        var comment = await AddPaymentCommentAsync(
            recurrentPayment.Claim,
            recurrentPayment.PaymentType,
            new ClaimPaymentRequest
            {
                Money = amount ?? recurrentPayment.PaymentAmount,
                ClaimId = claimId,
                ProjectId = projectId,
                PayerId = recurrentPayment.Claim.PlayerUserId,
                OperationDate = Now,
                FromRecurrentPaymentId = recurrentPaymentId,
                CommentText = $"Списание средств по подписке от {recurrentPayment.CreateDate:d}",
            });

        var api = GetApi(projectId, claimId);
        var result = await api.PayRecurrent(
            recurrentPayment.BankParentPayment!,
            comment.Finance.GetOrderId(),
            recurrentPayment.BankRecurrencyToken!,
            recurrentPayment.BankAdditional!,
            receipt);

        if (result.Status == PaymentInfoQueryStatus.Success)
        {
            logger.LogInformation("Recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId} has been successfully initiated as operation {financeOperationId}", recurrentPaymentId, claimId, projectId, comment.CommentId);
        }
        else
        {
            logger.LogError("Failed to initiate recurrent payment {recurrentPaymentId} for claim {claimId} to project {projectId} because {bankError}", recurrentPayment.RecurrentPaymentId, claimId, projectId, result.ErrorDescription);

            comment.Finance.State = FinanceOperationState.Declined;
            await UnitOfWork.SaveChangesAsync();
        }

        return comment.Finance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Task<FinanceOperation> IPaymentsService.RefundAsync(int projectId, int claimId, int operationId)
        => RefundAsync(projectId, claimId, operationId);

    private async Task<FinanceOperation> RefundAsync(int projectId, int claimId, int operationId, bool partial = false, int? amount = null)
    {
        var sourceFo = await LoadFinanceOperationAsync(projectId, claimId, operationId);

        if (sourceFo.OperationType != FinanceOperationType.Online)
        {
            logger.LogError("Finance operation {financeOperationId} is not online operation and can not be refunded", operationId);
            throw new PaymentException(sourceFo.Project, $"Finance operation {operationId} was not made online");
        }

        if (sourceFo.State != FinanceOperationState.Approved)
        {
            logger.LogError("Finance operation {financeOperationId} can not be refunded because it was not approved", operationId);
            throw new PaymentException(sourceFo.Project, $"Finance operation {operationId} was not approved");
        }

        if (partial)
        {
            throw new NotImplementedException("Partial refunds are not implemented yet");
        }

        // We have to check was operation already completely refunded or not
        var refundedFo = await UnitOfWork.GetDbSet<FinanceOperation>()
            .Where(fo => fo.RefundedOperationId == sourceFo.CommentId && fo.State == FinanceOperationState.Approved)
            .ToArrayAsync();
        var refundedSum = refundedFo.Sum(fo => fo.MoneyAmount);
        if (refundedSum != 0)
        {
            logger.LogError("Finance operation {financeOperationId} can not be refunded because is already refunded", operationId);
            throw new PaymentException(sourceFo.Project, $"Finance operation {operationId} is already refunded");
        }

        var comment = await AddPaymentCommentAsync(
            sourceFo.Claim,
            sourceFo.PaymentType!,
            new ClaimPaymentRequest
            {
                Refund = true,
                Money = sourceFo.MoneyAmount,
                ClaimId = claimId,
                ProjectId = projectId,
                PayerId = sourceFo.Claim.PlayerUserId,
                OperationDate = Now,
                FinanceOperationToRefundId = sourceFo.CommentId,
                FromRecurrentPaymentId = sourceFo.RecurrentPaymentId,
                CommentText = $"Оформлен возврат платежа от {sourceFo.Created:d} на сумму {sourceFo.MoneyAmount}",
            });

        var api = GetApi(projectId, claimId);
        var result = await api.Refund(sourceFo.GetOrderId(), false, null, null);

        if (result.Status == PaymentInfoQueryStatus.Success && result.CreatedRefund?.Status is not (null or RefundStatus.Error))
        {
            logger.LogInformation("Refund of payment {financeOperationId} for claim {claimId} to project {projectId} has been successfully initiated", sourceFo.CommentId, claimId, projectId);
            comment.Finance.BankRefundToken = result.CreatedRefund.Id;
            if (result.CreatedRefund.Status == RefundStatus.Completed)
            {
                comment.Finance.State = FinanceOperationState.Approved;
            }
        }
        else
        {
            logger.LogError("Failed to initiate refund of payment {financeOperationId} for claim {claimId} to project {projectId} because {bankError}", sourceFo.CommentId, claimId, projectId, result.ErrorDescription ?? "unknown problem");
            comment.Finance.BankRefundToken = result.CreatedRefund?.Id;
            comment.Finance.State = FinanceOperationState.Declined;
        }
        await UnitOfWork.SaveChangesAsync();

        if (comment.Finance.Approved)
        {
            await SendPaymentNotification(sourceFo.Claim, sourceFo.MoneyAmount, PaymentNotification.Refund);
        }

        return comment.Finance;
    }

    private abstract class PaymentRedirectUrl : ILinkable
    {
        /// <inheritdoc />
        public LinkType LinkType { get; protected set; }

        /// <inheritdoc />
        public string Identification { get; }

        /// <inheritdoc />
        public int? ProjectId { get; }

        protected PaymentRedirectUrl(int projectId, int claimId)
        {
            ProjectId = projectId;
            Identification = claimId.ToString();
        }
    }

    private class PaymentSuccessUrl : PaymentRedirectUrl
    {
        public PaymentSuccessUrl(int projectId, int claimId) : base(projectId, claimId) => LinkType = LinkType.PaymentSuccess;
    }

    private class PaymentFailUrl : PaymentRedirectUrl
    {
        public PaymentFailUrl(int projectId, int claimId) : base(projectId, claimId) => LinkType = LinkType.PaymentFail;
    }
}
