using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Notification;

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

      protected Comment AddCommentImpl(Claim claim,
          Comment parentComment,
          string commentText,
          bool isVisibleToPlayer,
          CommentExtraAction? extraAction = null)
      {


          var comment = CommentHelper.CreateCommentForDiscussion(claim.CommentDiscussion,
              CurrentUserId,
              Now,
              commentText,
              isVisibleToPlayer,
              parentComment,
              extraAction);

          claim.LastUpdateDateTime = Now;

          return comment;
      }

      protected async Task<FinanceOperationEmail> AcceptFeeImpl(string contents, DateTime operationDate, int feeChange,
      int money, PaymentType paymentType, Claim claim)
    {
      paymentType.EnsureActive();

      CheckOperationDate(operationDate);

      if (feeChange != 0 || money < 0)
      {
        claim.RequestAccess(CurrentUserId, acl => acl.CanManageMoney);
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
          claim.RequestAccess(CurrentUserId, acl => acl.CanManageMoney);
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

      comment.Finance = financeOperation;

      claim.FinanceOperations.Add(financeOperation);

      claim.UpdateClaimFeeIfRequired(operationDate);

      var email = await CreateClaimEmail<FinanceOperationEmail>(claim, contents,
        s => s.MoneyOperation,
        commentExtraAction: null,
        extraRecipients: new[] {paymentType.User});
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

      protected async Task<Claim> LoadClaimAsMaster(IClaimOperationRequest request, Expression<Func<ProjectAcl, bool>> accessType = null, ExtraAccessReason reason = ExtraAccessReason.None)
      {
          var claim = await ClaimsRepository.GetClaim(request.ProjectId, request.ClaimId);

          return  claim.RequestAccess(CurrentUserId, accessType, reason);
      }

      protected Task<Claim> LoadClaimAsMaster(IClaimOperationRequest request, ExtraAccessReason reason = ExtraAccessReason.None)
      {
          return LoadClaimAsMaster(request, acl => true, reason);
      }


      protected async Task<TEmail> CreateClaimEmail<TEmail>(
          [NotNull] Claim claim,
          [NotNull] string commentText,
          Func<UserSubscription, bool> subscribePredicate,
          CommentExtraAction? commentExtraAction,
          bool mastersOnly = false,
          IEnumerable<User> extraRecipients = null)
          where TEmail : ClaimEmailModel, new()
      {
          var initiator = await GetCurrentUser();
          if (claim == null) throw new ArgumentNullException(nameof(claim));
          if (commentText == null) throw new ArgumentNullException(nameof(commentText));
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
}
