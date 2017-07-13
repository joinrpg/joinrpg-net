using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  [UsedImplicitly]
  public class FinanceOperationsImpl : ClaimImplBase, IFinanceService
  {
    public FinanceOperationsImpl(IUnitOfWork unitOfWork, IEmailService emailService,
      IFieldDefaultValueGenerator fieldDefaultValueGenerator) : base(unitOfWork, emailService,
      fieldDefaultValueGenerator)
    {
    }

    public async Task FeeAcceptedOperation(int projectId, int claimId, string contents, DateTime operationDate,
  int feeChange, int money, int paymentTypeId)
    {
      var claim = await ClaimsRepository.GetClaim(projectId, claimId);
      var paymentType = claim.Project.PaymentTypes.Single(pt => pt.PaymentTypeId == paymentTypeId);

      var email = await AcceptFeeImpl(contents, operationDate, feeChange, money, paymentType, claim);

      await UnitOfWork.SaveChangesAsync();
      

      await EmailService.Email(email);

    }

    public async Task CreateCashPaymentType(int projectid, int targetUserId)
    {
      var project = await ProjectRepository.GetProjectForFinanceSetup(projectid);
      project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);
      project.RequestMasterAccess(targetUserId);

      var targetMaster = project.ProjectAcls.Single(a => a.UserId == targetUserId);

      if (targetMaster.GetCashPaymentType() != null)
      {
        throw new JoinRpgInvalidUserException();
      }

      project.PaymentTypes.Add(PaymentType.CreateCash(targetMaster.UserId));

      await UnitOfWork.SaveChangesAsync();
    }

    public async Task TogglePaymentActivness(int projectid, int paymentTypeId)
    {
      var project = await ProjectRepository.GetProjectForFinanceSetup(projectid);
      project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);

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

    public async Task CreateCustomPaymentType(int projectId, string name, int targetMasterId)
    {
      var project = await ProjectRepository.GetProjectForFinanceSetup(projectId);
      project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);
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

    public async Task EditCustomPaymentType(int projectId, int paymentTypeId, string name, bool isDefault)
    {
      var project = await ProjectRepository.GetProjectForFinanceSetup(projectId);
      project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);

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

    public async Task CreateFeeSetting(int projectId, int fee, DateTime startDate)
    {
      var project = await ProjectRepository.GetProjectForFinanceSetup(projectId);
      project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);

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

    public async Task DeleteFeeSetting(int projectid, int projectFeeSettingId)
    {
      var project = await ProjectRepository.GetProjectForFinanceSetup(projectid);
      project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);

      var feeSetting = project.ProjectFeeSettings.Single(pt => pt.ProjectFeeSettingId == projectFeeSettingId);

      if (feeSetting.StartDate < DateTime.UtcNow)
      {
        throw new CannotPerformOperationInPast();
      }

      UnitOfWork.GetDbSet<ProjectFeeSetting>().Remove(feeSetting);

      await UnitOfWork.SaveChangesAsync();
    }

    public async Task ChangeFee(int projectId, int claimId, int feeValue)
    {
      
      var claim = await ClaimsRepository.GetClaim(projectId, claimId);

      claim.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);

      AddCommentImpl(claim, null, feeValue.ToString(), isVisibleToPlayer: true,
        extraAction: CommentExtraAction.FeeChanged);

      claim.CurrentFee = feeValue;

      await UnitOfWork.SaveChangesAsync();
    }

    public async Task SaveGlobalSettings(int projectId, bool warnOnOverPayment)
    {
      var project = await ProjectRepository.GetProjectForFinanceSetup(projectId);
      project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);

      project.Details.FinanceWarnOnOverPayment = warnOnOverPayment;
      await UnitOfWork.SaveChangesAsync();
    }
  }
}
