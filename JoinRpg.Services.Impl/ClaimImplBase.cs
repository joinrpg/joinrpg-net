using System;
using System.Threading.Tasks;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  public abstract class ClaimImplBase : DbServiceImplBase
  {
    protected IEmailService EmailService { get; }
    protected IFieldDefaultValueGenerator FieldDefaultValueGenerator { get; }

    protected ClaimImplBase(IUnitOfWork unitOfWork, IEmailService emailService,
      IFieldDefaultValueGenerator fieldDefaultValueGenerator) : base(unitOfWork)
    {
      EmailService = emailService;
      FieldDefaultValueGenerator = fieldDefaultValueGenerator;
    }

    protected Comment AddCommentImpl(Claim claim, Comment parentComment, string commentText,
      bool isVisibleToPlayer, CommentExtraAction? extraAction)
    {
      if (!isVisibleToPlayer)
      {
        claim.RequestMasterAccess(CurrentUserId);
      }

      var comment = new Comment
      {
        CommentId = -1,
        ProjectId = claim.ProjectId,
        AuthorUserId = CurrentUserId,
        CommentDiscussionId = claim.CommentDiscussion.CommentDiscussionId,
        CommentText = new CommentText()
        {
          CommentId = -1,
          Text = new MarkdownString(commentText)
        },
        IsCommentByPlayer = claim.PlayerUserId == CurrentUserId,
        IsVisibleToPlayer = isVisibleToPlayer,
        Parent = parentComment,
        ExtraAction = extraAction,
        CreatedAt = Now,
        LastEditTime = Now
      };
      claim.CommentDiscussion.Comments.Add(comment);

      claim.LastUpdateDateTime = Now;

      return comment;
    }

    protected async Task<FinanceOperationEmail> AcceptFeeImpl(string contents, DateTime operationDate, int feeChange,
      int money, PaymentType paymentType, Claim claim)
    {
      paymentType.EnsureActive();

      if (operationDate > Now.AddDays(1)
      ) //TODO[UTC]: if everyone properly uses UTC, we don't have to do +1
      {
        throw new CannotPerformOperationInFuture();
      }

      if (feeChange != 0 || money < 0)
      {
        claim.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);
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
          claim.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);
        }
      }

      var comment = AddCommentImpl(claim, null, contents, isVisibleToPlayer: true, extraAction: null);

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
        OperationDate = operationDate
      };

      comment.Finance = financeOperation;

      claim.FinanceOperations.Add(financeOperation);

      claim.UpdateClaimFeeIfRequired(operationDate);

      var email = EmailHelpers.CreateClaimEmail<FinanceOperationEmail>(claim, contents,
        s => s.MoneyOperation,
        commentExtraAction: null,
        initiator: await UserRepository.GetById(CurrentUserId),
        extraRecipients: new[] {paymentType.User});
      email.FeeChange = feeChange;
      email.Money = money;
      return email;
    }
  }
}
