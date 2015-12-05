using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  [UsedImplicitly]
  public class FinanceOperationsImpl : ClaimImplBase, IFinanceService
  {
    public FinanceOperationsImpl(IUnitOfWork unitOfWork, IEmailService emailService) : base(unitOfWork, emailService)
    {
    }

    public async Task FeeAcceptedOperation(int projectId, int claimId, int currentUserId, string contents, DateTime operationDate,
  int feeChange, int money, int paymentTypeId)
    {
      var claim = await LoadClaim(projectId, claimId, currentUserId);
      var now = DateTime.UtcNow;
      var paymentType = claim.Project.PaymentTypes.Single(pt => pt.PaymentTypeId == paymentTypeId);

      paymentType.EnsureActive();

      if (operationDate > now.AddDays(1))//TODO[UTC]: if everyone properly uses UTC, we don't have to do +1
      {
        throw new CannotPerformOperationInFuture();
      }

      if (feeChange != 0 || money < 0)
      {
        claim.RequestMasterAccess(currentUserId, acl => acl.CanManageMoney);
      }
      var state = FinanceOperationState.Approved;

      if (paymentType.UserId != currentUserId)
      {
        if (claim.PlayerUserId == currentUserId)
        {
          //Player mark that he pay fee. Put this to moderation
          state = FinanceOperationState.Proposed;
        }
        else
        {
          claim.RequestMasterAccess(currentUserId, acl => acl.CanManageMoney);
        }
      }

      var comment = claim.AddCommentImpl(currentUserId, null, contents, now, isVisibleToPlayer:true, extraAction: null);

      var financeOperation = new FinanceOperation()
      {
        Created = now,
        FeeChange = feeChange,
        MoneyAmount = money,
        Changed = now,
        Claim = claim,
        Comment = comment,
        PaymentType = paymentType,
        State = state,
        ProjectId = projectId,
        OperationDate = operationDate
      };

      comment.Finance = financeOperation;

      claim.FinanceOperations.Add(financeOperation);

      if (claim.Project.ProjectFeeSettings.Any()    //If project has fee 
          && claim.CurrentFee == null  //and fee not already fixed for claim
          && claim.ClaimBalance() > claim.ClaimTotalFee(operationDate) //and current fee is payed in full
          )
      {
        claim.CurrentFee = claim.Project.CurrentFee(operationDate); //fix fee for claim
      }

      await UnitOfWork.SaveChangesAsync();
      var email = CreateClaimEmail<FinanceOperationEmail>(claim, currentUserId, contents, s => s.MoneyOperation,
        isVisibleToPlayer: true, commentExtraAction: null);

      await EmailService.Email(await email);

    }
  }
}
