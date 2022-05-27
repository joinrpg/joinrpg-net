using Microsoft.EntityFrameworkCore;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using PscbApi;
using PscbApi.Models;

namespace JoinRpg.Services.Impl;

/// <inheritdoc cref="IPaymentsService" />
public class PaymentsService : DbServiceImplBase, IPaymentsService
{

    private readonly IBankSecretsProvider _bankSecrets;
    private readonly IUriService _uriService;

    /// <inheritdoc />
    public PaymentsService(
        IUnitOfWork unitOfWork,
        IUriService uriService,
        IBankSecretsProvider bankSecrets,
        ICurrentUserAccessor currentUserAccessor)
        : base(unitOfWork, currentUserAccessor)
    {
        _bankSecrets = bankSecrets;
        _uriService = uriService;
    }


    private ApiConfiguration GetApiConfiguration(int projectId, int claimId)
    {
        return new ApiConfiguration
        {
            Debug = _bankSecrets.Debug,
            ApiEndpoint = _bankSecrets.ApiEndpoint,
            ApiDebugEndpoint = _bankSecrets.ApiDebugEndpoint,
            MerchantId = _bankSecrets.MerchantId,
            ApiKey = _bankSecrets.ApiKey,
            ApiDebugKey = _bankSecrets.ApiDebugKey,
            DefaultSuccessUrl = _uriService.Get(new PaymentSuccessUrl(projectId, claimId)),
            DefaultFailUrl = _uriService.Get(new PaymentFailUrl(projectId, claimId)),
        };
    }

    private BankApi GetApi(int projectId, int claimId)
        => new BankApi(GetApiConfiguration(projectId, claimId));

    private async Task<Claim> GetClaimAsync(int projectId, int claimId)
    {
        Claim claim = await UnitOfWork.GetClaimsRepository()
            .GetClaim(projectId, claimId);
        if (claim == null)
        {
            throw new JoinRpgEntityNotFoundException(claimId, nameof(Claim));
        }

        return claim;
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

        var onlinePaymentType =
            claim.Project.ActivePaymentTypes.SingleOrDefault(
                pt => pt.TypeKind == PaymentTypeKind.Online);
        if (onlinePaymentType == null)
        {
            throw new OnlinePaymentsNotAvailable(claim.Project);
        }

        if (request.Money <= 0)
        {
            throw new PaymentException(claim.Project, $"Money amount must be positive integer");
        }

        User user = await GetCurrentUser();

        var message = new PaymentMessage
        {
            Amount = request.Money,
            Details = $"Билет (организационный взнос) участника на {claim.Project.ProjectName}",
            CustomerAccount = CurrentUserId.ToString(),
            CustomerEmail = user.Email,
            CustomerPhone = user.Extra?.PhoneNumber,
            CustomerComment = request.CommentText,
            PaymentMethod = request.Method switch
            {
                PaymentMethod.BankCard => PscbPaymentMethod.BankCards,
                PaymentMethod.FastPaymentsSystem => PscbPaymentMethod.FastPaymentsSystem,
                _ => throw new NotSupportedException($"Payment method {request.Method} is not supported"),
            },
            SuccessUrl = _uriService.Get(new PaymentSuccessUrl(request.ProjectId, request.ClaimId)),
            FailUrl = _uriService.Get(new PaymentFailUrl(request.ProjectId, request.ClaimId)),
            Data = new PaymentMessageData
            {
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
                            Name = claim.Project.ProjectName,
                        }
                    }
                }
            }
        };

        // Creating request to bank
        PaymentRequestDescriptor result = await GetApi(request.ProjectId, request.ClaimId)
            .BuildPaymentRequestAsync(
                message,
                async () => (await AddPaymentCommentAsync(claim, onlinePaymentType, request))
                    .CommentId
                    .ToString()
                    .PadLeft(10, '0')
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
        };
        _ = UnitOfWork.GetDbSet<Comment>().Add(comment);
        await UnitOfWork.SaveChangesAsync();

        return comment;
    }

    private async Task<FinanceOperation> LoadFinanceOperationAsync(int projectId, int claimId, int operationId)
    {
        // Loading finance operation
        FinanceOperation fo = await UnitOfWork.GetDbSet<FinanceOperation>().FindAsync(operationId);

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

        if (fo.OperationType != FinanceOperationType.Online || fo.PaymentType?.TypeKind != PaymentTypeKind.Online)
        {
            throw new PaymentException(fo.Project, "Finance operation is not online payment");
        }

        return fo;
    }

    private async Task<FinanceOperation?> LoadLastUnapprovedFinanceOperationAsync(int projectId, int claimId)
    {
        return await (from fo in UnitOfWork.GetDbSet<FinanceOperation>()
                      join pt in UnitOfWork.GetDbSet<PaymentType>()
                          on fo.PaymentTypeId equals pt.PaymentTypeId
                      where fo.ProjectId == projectId
                          && fo.ClaimId == claimId
                          && fo.OperationType == FinanceOperationType.Online
                          && pt.TypeKind == PaymentTypeKind.Online
                          && fo.State == FinanceOperationState.Proposed
                      select fo)
            .Include(fo => fo.PaymentType)
            .OrderByDescending(fo => fo.Created)
            .FirstOrDefaultAsync();
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

        var api = GetApi(fo.ProjectId, fo.ClaimId);
        string orderIdStr = fo.CommentId.ToString().PadLeft(10, '0');

        // Asking bank
        PaymentInfo paymentInfo = await api.GetPaymentInfoAsync(
            PscbPaymentMethod.BankCards,
            orderIdStr);

        if (paymentInfo.Status == PaymentInfoQueryStatus.Failure
            && paymentInfo.ErrorCode == ApiErrorCode.UnknownPayment)
        {
            paymentInfo = await api.GetPaymentInfoAsync(
                PscbPaymentMethod.FastPaymentsSystem,
                orderIdStr);
        }

        // Updating status
        if (paymentInfo.Status == PaymentInfoQueryStatus.Success)
        {
            if (paymentInfo.ErrorCode == ApiErrorCode.UnknownPayment)
            {
                fo.State = FinanceOperationState.Declined;
                fo.Changed = Now;
            }
            else if (paymentInfo.ErrorCode == null)
            {
                UpdateFinanceOperationStatus(fo, paymentInfo.Payment);
                if (fo.State == FinanceOperationState.Approved)
                {
                    Claim claim = await GetClaimAsync(fo.ProjectId, fo.ClaimId);
                    claim.UpdateClaimFeeIfRequired(Now);
                }
            }
        }
        else if (paymentInfo.ErrorCode == ApiErrorCode.UnknownPayment)
        {
            fo.State = FinanceOperationState.Invalid;
            fo.Changed = Now;
        }
        else if (IsCurrentUserAdmin)
        {
            throw new PaymentException(
                fo.Project,
                $"Payment status check failed: {paymentInfo.ErrorDescription}");
        }

        // Saving if status was updated
        if (fo.State != FinanceOperationState.Proposed)
        {
            await UnitOfWork.SaveChangesAsync();
        }

        // TODO: Probably need to send some notifications?
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

