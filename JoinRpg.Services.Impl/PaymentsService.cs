using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using PscbApi;
using PscbApi.Models;

namespace JoinRpg.Services.Impl
{
    /// <inheritdoc cref="IPaymentsService" />
    public class PaymentsService : DbServiceImplBase, IPaymentsService
    {

        private readonly IBankSecretsProvider _bankSecrets;
        private readonly IUriService _uriService;

        /// <inheritdoc />
        public PaymentsService(
            IUnitOfWork unitOfWork,
            IUriService uriService,
            IBankSecretsProvider bankSecrets)
            : base(unitOfWork)
        {
            _bankSecrets = bankSecrets;
            _uriService = uriService;
        }


        private ApiConfiguration GetApiConfiguration(int projectId, int claimId)
        {
            return new ApiConfiguration
            {
#if DEBUG
                Debug = true,
#endif
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
                throw new JoinRpgEntityNotFoundException(claimId, nameof(Claim));
            return claim;
        }

        /// <inheritdoc />
        public async Task<ClaimPaymentContext> InitiateClaimPaymentAsync(ClaimPaymentRequest request)
        {
            // Loading claim
            var claim = await GetClaimAsync(request.ProjectId, request.ClaimId);

            // Checking access rights
            if (claim.PlayerUserId != CurrentUserId)
                throw new NoAccessToProjectException(claim.Project, CurrentUserId);

            PaymentType onlinePaymentType =
                claim.Project.ActivePaymentTypes.SingleOrDefault(
                    pt => pt.TypeKind == PaymentTypeKind.Online);
            if (onlinePaymentType == null)
                throw new OnlinePaymentsNotAvailable(claim.Project);

            if (request.Money <= 0)
                throw new PaymentException(claim.Project, $"Money amount must be positive integer");

            User user = await GetCurrentUser();

            var message = new PaymentMessage
            {
                Amount = request.Money,
                Details = $"Билет (организационный взнос) участника на «{claim.Project.ProjectName}»",
                CustomerAccount = CurrentUserId.ToString(),
                CustomerEmail = user.Email,
                CustomerPhone = user.Extra?.PhoneNumber,
                CustomerComment = request.CommentText,
                PaymentMethod = PscbPaymentMethod.BankCards,
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
                    async () => (await AddPaymentCommentAsync(claim.CommentDiscussion, onlinePaymentType, request))
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
            CommentDiscussion discussion,
            PaymentType paymentType,
            ClaimPaymentRequest request)
        {
            Comment comment = CommentHelper.CreateCommentForDiscussion(
                discussion,
                CurrentUserId,
                Now,
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
            UnitOfWork.GetDbSet<Comment>().Add(comment);
            await UnitOfWork.SaveChangesAsync();

            return comment;
        }

        private async Task<FinanceOperation> LoadFinanceOperationAsync(int projectId, int claimId, int operationId)
        {
            // Loading finance operation
            FinanceOperation fo = await UnitOfWork.GetDbSet<FinanceOperation>().FindAsync(operationId);

            if (fo == null)
                throw new JoinRpgEntityNotFoundException(operationId, nameof(FinanceOperation));
            if (fo.ClaimId != claimId)
                throw new JoinRpgEntityNotFoundException(claimId, nameof(Claim));
            if (fo.ProjectId != projectId)
                throw new JoinRpgEntityNotFoundException(projectId, nameof(Project));
            if (fo.OperationType != FinanceOperationType.Online || fo.PaymentType?.TypeKind != PaymentTypeKind.Online)
                throw new PaymentException(fo.Project, "Finance operation is not online payment");

            return fo;
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
                // Something wrong
                case PaymentStatus.Expired:
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

        /// <inheritdoc />
        public async Task UpdateClaimPaymentAsync(int projectId, int claimId, int orderId)
        {
            var fo = await LoadFinanceOperationAsync(projectId, claimId, orderId);

            if (fo.State == FinanceOperationState.Proposed)
            {
                // Asking bank
                PaymentInfo paymentInfo = await GetApi(projectId, claimId)
                    .GetPaymentInfoAsync(new PaymentInfoQuery
                    {
                        OrderId = orderId.ToString().PadLeft(10, '0')
                    });

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
                    }
                }
                else if (IsCurrentUserAdmin)
                    throw new PaymentException(fo.Project, $"Payment status check failed: {paymentInfo.ErrorDescription}");

                // Saving if status was updated
                if (fo.State != FinanceOperationState.Proposed)
                    await UnitOfWork.SaveChangesAsync();

                // TODO: Probably need to send some notifications?
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
            public PaymentSuccessUrl(int projectId, int claimId) : base(projectId, claimId)
            {
                LinkType = LinkType.PaymentSuccess;
            }
        }

        private class PaymentFailUrl : PaymentRedirectUrl
        {
            public PaymentFailUrl(int projectId, int claimId) : base(projectId, claimId)
            {
                LinkType = LinkType.PaymentFail;
            }
        }

    }

}
