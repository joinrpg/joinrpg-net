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
      var claim = await ClaimsRepository.GetClaim(projectId, claimId);
      var now = DateTime.UtcNow;
      var paymentType = claim.Project.PaymentTypes.Single(pt => pt.PaymentTypeId == paymentTypeId);

      paymentType.EnsureActive();

      if (operationDate > now.AddDays(1)) //TODO[UTC]: if everyone properly uses UTC, we don't have to do +1
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

      claim.UpdateClaimFeeIfRequired(operationDate);

      await UnitOfWork.SaveChangesAsync();
      var email = await CreateClaimEmail<FinanceOperationEmail>(claim, currentUserId, contents, s => s.MoneyOperation,
        isVisibleToPlayer: true, commentExtraAction: null, extraRecepients: new [] { paymentType.User});
      email.FeeChange = feeChange;
      email.Money = money;

      await EmailService.Email(email);

    }

    public async Task CreateCashPaymentType(int projectid, int currentUserId, int targetUserId)
    {
      var project = await ProjectRepository.GetProjectForFinanceSetup(projectid);
      project.RequestMasterAccess(currentUserId, acl => acl.CanManageMoney);
      project.RequestMasterAccess(targetUserId);

      var targetMaster = project.ProjectAcls.Single(a => a.UserId == targetUserId);

      if (targetMaster.GetCashPaymentType() != null)
      {
        throw new JoinRpgInvalidUserException();
      }

      project.PaymentTypes.Add(PaymentType.CreateCash(targetMaster.UserId));

      await UnitOfWork.SaveChangesAsync();
    }

    public async Task TogglePaymentActivness(int projectid, int currentUserId, int paymentTypeId)
    {
      var project = await ProjectRepository.GetProjectForFinanceSetup(projectid);
      project.RequestMasterAccess(currentUserId, acl => acl.CanManageMoney);

      var paymentType = project.PaymentTypes.Single(pt => pt.PaymentTypeId == paymentTypeId);

      if (paymentType.IsActive)
      {
        SmartDelete(paymentType);
      }
      else
      {
        paymentType.IsActive = true;
      }
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task CreateCustomPaymentType(int projectId, int currentUserId, string name, int targetMasterId)
    {
      var project = await ProjectRepository.GetProjectForFinanceSetup(projectId);
      project.RequestMasterAccess(currentUserId, acl => acl.CanManageMoney);
      project.RequestMasterAccess(targetMasterId);

      project.PaymentTypes.Add(new PaymentType()
      {
        IsActive = true,
        IsCash = false,
        IsDefault = project.PaymentTypes.All(pt => !pt.IsDefault),
        Name = Required(name),
        UserId = targetMasterId,
        ProjectId = projectId
      });

      await UnitOfWork.SaveChangesAsync();
    }

    public async Task EditCustomPaymentType(int projectId, int currentUserId, int paymentTypeId, string name, bool isDefault)
    {
      var project = await ProjectRepository.GetProjectForFinanceSetup(projectId);
      project.RequestMasterAccess(currentUserId, acl => acl.CanManageMoney);

      var paymentType = project.PaymentTypes.Single(pt => pt.PaymentTypeId == paymentTypeId);

      paymentType.IsActive = true;
      paymentType.Name = Required(name);

      if (isDefault && !paymentType.IsDefault)
      {
        foreach (var oldDefault in project.PaymentTypes.Where(pt => pt.IsDefault))
        {
          oldDefault.IsDefault = false;
        }
      }
      paymentType.IsDefault = isDefault;

      await UnitOfWork.SaveChangesAsync();
    }

    public async Task CreateFeeSetting(int projectId, int currentUserId, int fee, DateTime startDate)
    {
      var project = await ProjectRepository.GetProjectForFinanceSetup(projectId);
      project.RequestMasterAccess(currentUserId, acl => acl.CanManageMoney);

      if (startDate < DateTime.UtcNow.Date.AddDays(-1)) 
      {
        throw new CannotPerformOperationInPast();
      }

      project.ProjectFeeSettings.Add(new ProjectFeeSetting()
      {
        Fee = fee,
        StartDate = startDate,
        ProjectId = projectId
      });

      var firstFee = project.ProjectFeeSettings.OrderBy(s => s.StartDate).First();
      firstFee.StartDate = project.CreatedDate;

      await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteFeeSetting(int projectid, int currentUserId, int projectFeeSettingId)
    {
      var project = await ProjectRepository.GetProjectForFinanceSetup(projectid);
      project.RequestMasterAccess(currentUserId, acl => acl.CanManageMoney);

      var feeSetting = project.ProjectFeeSettings.Single(pt => pt.ProjectFeeSettingId == projectFeeSettingId);

      if (feeSetting.StartDate < DateTime.UtcNow)
      {
        throw new CannotPerformOperationInPast();
      }

      UnitOfWork.GetDbSet<ProjectFeeSetting>().Remove(feeSetting);

      await UnitOfWork.SaveChangesAsync();
    }
  }
}
