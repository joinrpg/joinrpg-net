using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Helpers;
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
            if (!(claim.HasAccess(CurrentUserId, ExtraAccessReason.Player)
                || claim.HasMasterAccess(CurrentUserId, acl => acl.CanManageMoney)
                || IsCurrentUserAdmin))
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
                        Items = new List<ReceiptItem>
                        {
                            new ReceiptItem
                            {
                                ObjectType = PaymentObjectType.Commodity,
                                PaymentType = ItemPaymentType.Advance,
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
//                    () => Task.FromResult(new Random().Next().ToString())
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
                                                    
        /// <inheritdoc />
        public async Task SetClaimPaymentResultAsync(ClaimPaymentResultContext result)
        {
            // Loading finance operation
            FinanceOperation fo = await UnitOfWork.GetDbSet<FinanceOperation>().FindAsync(result.OrderId);

            if (fo == null)
                throw new JoinRpgEntityNotFoundException(result.OrderId, nameof(FinanceOperation));
            if (fo.ClaimId != result.ClaimId)
                throw new JoinRpgEntityNotFoundException(result.ClaimId, nameof(Claim));
            if (fo.ProjectId != result.ProjectId)
                throw new JoinRpgEntityNotFoundException(result.ProjectId, nameof(Project));
            if (fo.OperationType != FinanceOperationType.Online || fo.PaymentType?.TypeKind != PaymentTypeKind.Online)
                throw new PaymentException(fo.Project, "Finance operation is not online payment");
            if (fo.State != FinanceOperationState.Proposed)
                throw new OnlinePaymentUnexpectedStateException(fo, FinanceOperationState.Proposed);

            // Applying new state
            var paymenInfo = await GetApi(result.ProjectId, result.ClaimId)
                .GetPaymentInfoAsync(new PaymentInfoQuery
                {
                    OrderId = result.OrderId.ToString().PadLeft(10, '0')
                });

            if (paymenInfo.Status == PaymentInfoQueryStatus.Success)
                UpdateFinanceOperationStatus(fo, paymenInfo.Payment);
            else if (IsCurrentUserAdmin)
                throw new PaymentException(fo.Project, $"Payment status check failed: {paymenInfo.ErrorDescription}");

            if (fo.State != FinanceOperationState.Proposed)
                await UnitOfWork.SaveChangesAsync();

            // TODO: Probably need to send some notifications?
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
            // Loading claim
            var claim = await GetClaimAsync(projectId, claimId);

            // Checking access rights
            if (!(claim.HasAccess(CurrentUserId, ExtraAccessReason.Player)
                || claim.HasMasterAccess(CurrentUserId, acl => acl.CanManageMoney)
                || IsCurrentUserAdmin))
                throw new NoAccessToProjectException(claim.Project, CurrentUserId);

            // Finance operation
            FinanceOperation fo = claim.FinanceOperations.SingleOrDefault(f => f.CommentId == orderId);
            if (fo == null)
                throw new JoinRpgEntityNotFoundException(orderId, nameof(FinanceOperation));
            if (fo.OperationType != FinanceOperationType.Online || fo.PaymentType?.TypeKind != PaymentTypeKind.Online)
                throw new PaymentException(fo.Project, "Finance operation is not online payment");

            if (fo.State == FinanceOperationState.Proposed)
            {
                // Asking bank
                PaymentInfo paymentInfo = await GetApi(projectId, claimId)
                    .GetPaymentInfoAsync(new PaymentInfoQuery {OrderId = orderId.ToString().PadLeft(10, '0') });

                // If payment was not found marking it as declined
                if (paymentInfo.ErrorCode == ApiErrorCode.UnknownPayment)
                {
                    fo.State = FinanceOperationState.Declined;
                    fo.Changed = Now;
                }
                else
                    UpdateFinanceOperationStatus(fo, paymentInfo.Payment);

                if (fo.State != FinanceOperationState.Proposed)
                    await UnitOfWork.SaveChangesAsync();
            }
        }
        
        private async Task<Tuple<Comment, Comment>> AddTransferCommentsAsync(
            CommentDiscussion discussionFrom,
            CommentDiscussion discussionTo,
            ClaimPaymentTransferRequest request)
        {
            // Comment to source claim
            Comment commentFrom = CommentHelper.CreateCommentForDiscussion(
                discussionFrom,
                CurrentUserId,
                Now,
                request.CommentText,
                true,
                null);
            commentFrom.Finance = new FinanceOperation
            {
                OperationType = FinanceOperationType.TransferTo,
                MoneyAmount = -request.Money,
                OperationDate = request.OperationDate,
                ProjectId = request.ProjectId,
                ClaimId = request.ClaimId,
                LinkedClaimId = request.ToClaimId,
                Created = Now,
                Changed = Now,
                State = FinanceOperationState.Approved,
            };
            UnitOfWork.GetDbSet<Comment>().Add(commentFrom);

            // Comment to destination claim
            Comment commentTo = CommentHelper.CreateCommentForDiscussion(
                discussionTo,
                CurrentUserId,
                Now,
                request.CommentText,
                true,
                null);
            commentTo.Finance = new FinanceOperation
            {
                OperationType = FinanceOperationType.TransferFrom,
                MoneyAmount = request.Money,
                OperationDate = request.OperationDate,
                ProjectId = request.ProjectId,
                ClaimId = request.ToClaimId,
                LinkedClaimId = request.ClaimId,
                Created = Now,
                Changed = Now,
                State = FinanceOperationState.Approved,
            };

            await UnitOfWork.SaveChangesAsync();

            return Tuple.Create(commentFrom, commentTo);
        }

        /// <inheritdoc />
        public async Task TransferPaymentAsync(ClaimPaymentTransferRequest request)
        {
            var claimFrom = await GetClaimAsync(request.ProjectId, request.ClaimId);
            var claimTo = await GetClaimAsync(request.ProjectId, request.ToClaimId);

            // Checking access rights
            if (!(claimFrom.HasMasterAccess(CurrentUserId, acl => acl.CanManageMoney)
                || IsCurrentUserAdmin))
                throw new NoAccessToProjectException(claimFrom.Project, CurrentUserId);

            // Checking money amount
            var availableMoney = claimFrom.GetPaymentSum();
            if (availableMoney < request.Money)
                throw new PaymentException(claimFrom.Project, $"Not enough money at claim {claimFrom.Name} to perform transfer");

            // Adding comments
            await AddTransferCommentsAsync(
                claimFrom.CommentDiscussion,
                claimTo.CommentDiscussion,
                request);
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
