using System.Linq.Expressions;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl;

public abstract class ClaimImplBase : DbServiceImplBase
{
    protected IProjectMetadataRepository ProjectMetadataRepository { get; }

    protected IEmailService EmailService { get; }

    protected ClaimImplBase(IUnitOfWork unitOfWork,
        IEmailService emailService,
        ICurrentUserAccessor currentUserAccessor,
        IProjectMetadataRepository projectMetadataRepository) : base(unitOfWork, currentUserAccessor)
    {
        EmailService = emailService;
        ProjectMetadataRepository = projectMetadataRepository;
    }

    protected Comment AddCommentImpl(Claim claim,
        Comment? parentComment,
        string commentText,
        bool isVisibleToPlayer,
        CommentExtraAction? extraAction = null)
    {
        var comment = CommentHelper.CreateCommentForClaim(claim,
            CurrentUserId,
            Now,
            commentText,
            isVisibleToPlayer,
            parentComment,
            extraAction);
        return comment;
    }

    protected async Task<FinanceOperationEmail> AcceptFeeImpl(string contents, DateTime operationDate, int feeChange,
    int money, PaymentType paymentType, Claim claim)
    {

        var projectInfo = await ProjectMetadataRepository.GetProjectMetadata(new(claim.ProjectId));
        _ = paymentType.EnsureActive();

        CheckOperationDate(operationDate);

        if (feeChange != 0 || money < 0)
        {
            _ = claim.RequestAccess(CurrentUserId, acl => acl.CanManageMoney);
        }
        var state = FinanceOperationState.Approved;

        if (paymentType.UserId != CurrentUserId)
        {
            if (claim.PlayerUserId == CurrentUserId)
            {
                //Player mark that he pay fee. Put this to moderation
                state = FinanceOperationState.Proposed;
            }
            else
            {
                _ = claim.RequestAccess(CurrentUserId, acl => acl.CanManageMoney);
            }
        }

        var comment = AddCommentImpl(claim, null, contents, isVisibleToPlayer: true);

        var financeOperation = new FinanceOperation()
        {
            Created = Now,
            FeeChange = feeChange,
            MoneyAmount = money,
            Changed = Now,
            Claim = claim,
            Comment = comment,
            PaymentType = paymentType,
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

        claim.UpdateClaimFeeIfRequired(operationDate);

        var email = await CreateClaimEmail<FinanceOperationEmail>(claim, contents,
          s => s.MoneyOperation,
          commentExtraAction: null,
          extraRecipients: new[] { paymentType.User });
        email.FeeChange = feeChange;
        email.Money = money;
        return email;
    }

    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
    protected void CheckOperationDate(DateTime operationDate)
    {
        if (operationDate > Now.AddDays(1)
        ) //TODO[UTC]: if everyone properly uses UTC, we don't have to do +1
        {
            throw new CannotPerformOperationInFuture();
        }
    }

    protected Task<(Claim, ProjectInfo)> LoadClaimAsMaster(IClaimOperationRequest request, Expression<Func<ProjectAcl, bool>> accessType, ExtraAccessReason reason = ExtraAccessReason.None)
        => LoadClaimAsMaster(request.ProjectIdentification, request.ClaimId, accessType, reason);

    protected Task<(Claim, ProjectInfo)> LoadClaimAsMaster(ProjectIdentification projectId, int claimId, ExtraAccessReason reason = ExtraAccessReason.None)
        => LoadClaimAsMaster(projectId, claimId, acl => true, reason);

    protected Task<(Claim, ProjectInfo)> LoadClaimAsMaster(IClaimOperationRequest request, ExtraAccessReason reason = ExtraAccessReason.None) => LoadClaimAsMaster(request, acl => true, reason);


    protected async Task<(Claim, ProjectInfo)> LoadClaimAsMaster(ProjectIdentification projectId, int claimId, Expression<Func<ProjectAcl, bool>> accessType, ExtraAccessReason reason = ExtraAccessReason.None)
    {
        var claim = await ClaimsRepository.GetClaim(projectId.Value, claimId);
        var projectInfo = await ProjectMetadataRepository.GetProjectMetadata(projectId);

        return (claim.RequestAccess(CurrentUserId, accessType, reason), projectInfo);
    }

    protected async Task<TEmail> CreateClaimEmail<TEmail>(
        [NotNull] Claim claim,
        [NotNull] string commentText,
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
