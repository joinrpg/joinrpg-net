using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.Notifications;
using JoinRpg.PrimitiveTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl;

internal abstract class ClaimImplBase(IUnitOfWork unitOfWork,
    IEmailService emailService,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository,
    CommentHelper commentHelper
    ) : DbServiceImplBase(unitOfWork, currentUserAccessor)
{
    protected CommentHelper CommentHelper { get; } = commentHelper;
    protected IProjectMetadataRepository ProjectMetadataRepository { get; } = projectMetadataRepository;

    protected IEmailService EmailService { get; } = emailService;

    protected ClaimSimpleChangedNotification AcceptFeeImpl(string contents,
                                                           DateTime operationDate,
                                                           int money,
                                                           PaymentTypeInfo paymentType,
                                                           Claim claim,
                                                           ProjectInfo projectInfo)
    {

        CheckOperationDate(operationDate);

        paymentType.EnsureActive();

        bool playerChange;

        if (money < 0)
        {
            projectInfo.RequestMasterAccess(currentUserAccessor, Permission.CanManageMoney);
            playerChange = false;
        }
        else if (claim.PlayerUserId == CurrentUserId)
        {
            playerChange = true;
        }
        else
        {

            if (paymentType.User.UserId != CurrentUserId)
            {
                projectInfo.RequestMasterAccess(currentUserAccessor, Permission.CanManageMoney);
            }
            else
            {
                projectInfo.RequestMasterAccess(currentUserAccessor);
            }
            playerChange = false;
        }

        var commentAction = money < 0 ? CommentExtraAction.RefundFee : CommentExtraAction.PaidFee;

        projectInfo.EnsureProjectActive();

        ClaimOperationType claimOperationType = playerChange ? ClaimOperationType.PlayerChange : ClaimOperationType.MasterVisibleChange;
        var state = playerChange ? FinanceOperationState.Proposed : FinanceOperationState.Approved;

        var (comment, email) = CommentHelper.AddClaimCommentWithNotification(contents, claim, projectInfo, commentAction, claimOperationType, Now);

        email = email with
        {
            Money = money,
            ExtraSubscribers = [new NotificationRecepient(paymentType.User, SubscriptionReason.Finance)],
        };

        var financeOperation = new FinanceOperation()
        {
            Created = Now,
            MoneyAmount = money,
            Changed = Now,
            Claim = claim,
            Comment = comment,
            PaymentTypeId = paymentType.PaymentTypeId.PaymentTypeId,
            State = state,
            ProjectId = claim.ProjectId,
            OperationDate = operationDate,
        };
        // TODO: Remove when complete Refunds be available
        if (money > 0)
        {
            financeOperation.OperationType = FinanceOperationType.Submit;
        }
        else if (money < 0)
        {
            financeOperation.OperationType = FinanceOperationType.Refund;
        }
        else
        {
            throw new PaymentException(claim.Project, "Submit or Refund sum could not be 0");
        }

        comment.Finance = financeOperation;

        claim.FinanceOperations.Add(financeOperation);

        claim.UpdateClaimFeeIfRequired(operationDate, projectInfo);

        return email;
    }

    protected void CheckOperationDate(DateTime operationDate)
    {
        if (operationDate > Now.AddDays(1)
        ) //TODO[UTC]: if everyone properly uses UTC, we don't have to do +1
        {
            throw new CannotPerformOperationInFuture();
        }
    }

    protected Task<(Claim, ProjectInfo)> LoadClaimAsMaster(IClaimOperationRequest request, Permission permission = Permission.None, ExtraAccessReason reason = ExtraAccessReason.None)
        => LoadClaimAsMaster(new ClaimIdentification(request.ProjectIdentification, request.ClaimId), permission, reason);


    protected async Task<(Claim, ProjectInfo)> LoadClaimAsMaster(ClaimIdentification claimId, Permission permission = Permission.None, ExtraAccessReason reason = ExtraAccessReason.None)
    {
        var claim = await ClaimsRepository.GetClaim(claimId);
        var projectInfo = await ProjectMetadataRepository.GetProjectMetadata(claimId.ProjectId);

        return (claim.RequestAccess(CurrentUserId, permission, reason), projectInfo);
    }

    protected async Task<(Claim, ProjectInfo)> LoadClaimAsPlayer(ClaimIdentification claimId)
    {
        var claim = await ClaimsRepository.GetClaim(claimId);
        var projectInfo = await ProjectMetadataRepository.GetProjectMetadata(claimId.ProjectId);

        if (claim?.PlayerUserId != CurrentUserId)
        {
            throw new PlayerOnlyException(claimId, CurrentUserId);
        }

        return (claim, projectInfo);
    }

    protected async Task<TEmail> CreateClaimEmail<TEmail>(
        Claim claim,
        string commentText,
        Func<UserSubscription, bool> subscribePredicate,
        CommentExtraAction? commentExtraAction,
        bool mastersOnly = false,
        IEnumerable<User?>? extraRecipients = null)
        where TEmail : ClaimEmailModel, new()
    {
        var initiator = await GetCurrentUser();
        if (claim == null)
        {
            throw new ArgumentNullException(nameof(claim));
        }

        if (commentText == null)
        {
            throw new ArgumentNullException(nameof(commentText));
        }

        var subscriptions =
            claim.GetSubscriptions(subscribePredicate, extraRecipients ?? Enumerable.Empty<User>(),
                mastersOnly).ToList();
        return new TEmail()
        {
            Claim = claim,
            ProjectName = claim.Project.ProjectName,
            Initiator = initiator,
            InitiatorType = initiator.UserId == claim.PlayerUserId ? ParcipantType.Player : ParcipantType.Master,
            Recipients = subscriptions,
            Text = new MarkdownString(commentText),
            CommentExtraAction = commentExtraAction,
        };
    }
}
