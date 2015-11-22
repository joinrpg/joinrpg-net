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

      if (operationDate > now)
      {
        throw new CannotPerformOperationInFuture();
      }

      if (feeChange != 0)
      {
        claim.RequestMasterAccess(currentUserId, acl => acl.CanManageMoney);
      }

      if (paymentType.IsCash)
      {
        claim.RequestMasterAccess(currentUserId, acl => acl.CanAcceptCash);
      }
      else
      {
        if (paymentType.UserId != currentUserId)
        {
          throw new NoAccessToProjectException(paymentType, currentUserId);
        }
      }

      var comment = claim.AddCommentImpl(currentUserId, null, contents, now, isVisibleToPlayer:true, isMyClaim:false);

      var financeOperation = new FinanceOperation()
      {
        Created = now,
        FeeChange = feeChange,
        MoneyAmount = money,
        Changed = now,
        Claim = claim,
        Comment = comment,
        MasterUserId = currentUserId,
        PaymentType = paymentType,
        State = FinanceOperationState.Approved,
        ProjectId = projectId,
        OperationDate = operationDate
      };

      comment.Finance = financeOperation;

      claim.FinanceOperations.Add(financeOperation);
      await UnitOfWork.SaveChangesAsync();
      var email = CreateClaimEmail<FinanceOperationEmail>(claim, currentUserId, contents, s => s.MoneyOperation);

      await EmailService.Email(await email);

    }
  }
}
