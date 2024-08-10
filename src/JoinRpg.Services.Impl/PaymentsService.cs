using System.Data.Entity;
using System.Runtime.CompilerServices;
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
            OperationType = FinanceOperationType.Online,
            PaymentTypeId = paymentType.PaymentTypeId,
            MoneyAmount = request.Money,
            OperationDate = request.OperationDate,
            ProjectId = request.ProjectId,
            ClaimId = request.ClaimId,
            Created = Now,
            Changed = Now,
            State = FinanceOperationState.Proposed,
            RecurrentPaymentId = recurrentPayment?.RecurrentPaymentId,
        };
        _ = UnitOfWork.GetDbSet<Comment>().Add(comment);
        await UnitOfWork.SaveChangesAsync();

        return comment;
    }

    private async Task<FinanceOperation> LoadFinanceOperationAsync(int projectId, int claimId, int operationId)
    {
        // Loading finance operation
        FinanceOperation fo = await UnitOfWork.GetDbSet<FinanceOperation>()
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

        if (fo.OperationType != FinanceOperationType.Online || fo.PaymentType?.TypeKind.IsOnline() != true)
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

        logger.LogInformation("Updating online payment {paymentId} for claim {claimId} to project {projectId}", fo.CommentId, fo.ClaimId, fo.ProjectId);

        var api = GetApi(fo.ProjectId, fo.ClaimId);
        var orderIdStr = fo.GetOrderId();

        // Asking bank
        PaymentInfo paymentInfo = await api.GetPaymentInfoAsync(orderIdStr);

        RecurrentPayment? recurrentPayment = fo.RecurrentPayment;
        if (recurrentPayment is null && fo.RecurrentPaymentId.HasValue)
        {
            recurrentPayment = await UnitOfWork.GetDbSet<RecurrentPayment>().FindAsync(fo.RecurrentPaymentId.Value);
        }

        // Updating status
        if (paymentInfo.Status == PaymentInfoQueryStatus.Success)
        {
            if (paymentInfo.ErrorCode == ApiErrorCode.UnknownPayment)
            {
                logger.LogError("Online payment {paymentId} for claim {claimId} to project {projectId} has failed", fo.CommentId, fo.ClaimId, fo.ProjectId);
                fo.State = FinanceOperationState.Declined;
                fo.Changed = Now;

                if (recurrentPayment?.Status is RecurrentPaymentStatus.Created)
                {
                    recurrentPayment.CloseDate = Now;
                    recurrentPayment.Status = RecurrentPaymentStatus.Failed;
                }
            }
            else if (paymentInfo.ErrorCode == null)
            {
                UpdateFinanceOperationStatus(fo, paymentInfo.Payment);
                if (fo.State == FinanceOperationState.Approved)
                {
                    logger.LogInformation("Online payment {paymentId} for claim {claimId} to project {projectId} has been successfully performed", fo.CommentId, fo.ClaimId, fo.ProjectId);

                    Claim claim = await GetClaimAsync(fo.ProjectId, fo.ClaimId);
                    var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(fo.ProjectId));

                    if (recurrentPayment?.Status is RecurrentPaymentStatus.Created)
                    {
                        recurrentPayment.BankRecurrencyToken = paymentInfo.Payment.RecurrentPaymentToken;
                        if (string.IsNullOrEmpty(recurrentPayment.BankRecurrencyToken))
                        {
                            recurrentPayment.BankRecurrencyToken = null;
                            recurrentPayment.CloseDate = Now;
                            recurrentPayment.Status = RecurrentPaymentStatus.Failed;
                        }
                        else
                        {
                            recurrentPayment.BankParentPayment = orderIdStr;
                            recurrentPayment.Status = RecurrentPaymentStatus.Initialization;
                        }
                    }

                    claim.UpdateClaimFeeIfRequired(Now, projectInfo);

                    await SendPaymentNotification(claim, fo.MoneyAmount, fo.RecurrentPaymentId.HasValue);
                }
                else if (recurrentPayment?.Status is RecurrentPaymentStatus.Created)
                {
                    recurrentPayment.CloseDate = Now;
                    recurrentPayment.Status = RecurrentPaymentStatus.Failed;
                }
            }
        }
        else if (paymentInfo.ErrorCode == ApiErrorCode.UnknownPayment)
        {
            fo.State = FinanceOperationState.Invalid;
            fo.Changed = Now;

            if (recurrentPayment?.Status is RecurrentPaymentStatus.Created)
            {
                recurrentPayment.CloseDate = Now;
                recurrentPayment.Status = RecurrentPaymentStatus.Failed;
            }
        }
        else if (IsCurrentUserAdmin)
        {
            throw new PaymentException(
                fo.Project,
                $"Payment status check failed: {paymentInfo.ErrorDescription}");
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
            }
            else
            {
                logger.LogError("Recurrent payment {recurrentPaymentId} setup for claim {claimId} to project {projectId} has failed", recurrentPayment.RecurrentPaymentId, fo.ClaimId, fo.ProjectId);
                recurrentPayment.Status = RecurrentPaymentStatus.Failed;
                recurrentPayment.CloseDate = Now;
            }

            await UnitOfWork.SaveChangesAsync();
        }
    }

    private async Task SendPaymentNotification(Claim claim, int sum, bool recurrent)
    {
        try
        {
            var subscriptions = claim.GetSubscriptions(p => p.MoneyOperation, [], mastersOnly: true).ToList();
            var email = new FinanceOperationEmail()
            {
                Claim = claim,
                ProjectName = claim.Project.ProjectName,
                Initiator = claim.Player,
                InitiatorType = ParcipantType.Player,
                Recipients = subscriptions,
                //TODO[Localize]
                Text = new MarkdownString(recurrent
                    ? $"Списание по подписке на сумму {sum} подтверждено"
                    : $"Онлайн-оплата на сумму {sum} подтверждена"),
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
    public async Task<FinanceOperation?> PerformRecurrentPayment(int projectId, int claimId, int recurrentPaymentId, int? amount, bool internalCall = false)
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
                Method = PaymentMethod.FastPaymentsSystem,
                Money = amount ?? recurrentPayment.PaymentAmount,
                ClaimId = claimId,
                ProjectId = projectId,
                PayerId = recurrentPayment.Claim.PlayerUserId,
                OperationDate = Now,
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
