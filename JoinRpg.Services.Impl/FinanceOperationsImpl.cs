using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Email;
using JoinRpg.Services.Interfaces.Notification;

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

    public async Task FeeAcceptedOperation(FeeAcceptedOperationRequest request)
    {
        var claim = await LoadClaimAsMaster(request, ExtraAccessReason.Player);
      var paymentType = claim.Project.PaymentTypes.Single(pt => pt.PaymentTypeId == request.PaymentTypeId);

      var email = await AcceptFeeImpl(request.Contents, request.OperationDate, request.FeeChange, request.Money, paymentType, claim);

      await UnitOfWork.SaveChangesAsync();
      
      await EmailService.Email(email);

    }

      public async Task CreateCashPaymentType(int projectid, int targetUserId)
    {
      var project = await ProjectRepository.GetProjectForFinanceSetup(projectid);
      project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);
      project.RequestMasterAccess(targetUserId);

      var targetMaster = project.ProjectAcls.Single(a => a.UserId == targetUserId);

        if (project.GetCashPaymentType(targetUserId) != null)
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
        ProjectId = projectId,
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

      public async Task CreateFeeSetting(CreateFeeSettingRequest request)
      {
          var project = await ProjectRepository.GetProjectForFinanceSetup(request.ProjectId);
          project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);

          if (request.StartDate < DateTime.UtcNow.Date.AddDays(-1))
          {
              throw new CannotPerformOperationInPast();
          }

          if (!project.Details.PreferentialFeeEnabled && request.PreferentialFee != null)
          {
              throw new PreferentialFeeNotEnabled();
          }

          project.ProjectFeeSettings.Add(new ProjectFeeSetting()
          {
              Fee = request.Fee,
              StartDate = request.StartDate,
              ProjectId = request.ProjectId,
              PreferentialFee = request.PreferentialFee,
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

      claim.RequestAccess(CurrentUserId, acl => acl.CanManageMoney);

      AddCommentImpl(claim, null, feeValue.ToString(), isVisibleToPlayer: true,
        extraAction: CommentExtraAction.FeeChanged);

      claim.CurrentFee = feeValue;

      await UnitOfWork.SaveChangesAsync();
    }

      public async Task SaveGlobalSettings(SetFinanceSettingsRequest request)
      {
          var project = await ProjectRepository.GetProjectForFinanceSetup(request.ProjectId);
          project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);

          project.Details.FinanceWarnOnOverPayment = request.WarnOnOverPayment;
          project.Details.PreferentialFeeEnabled = request.PreferentialFeeEnabled;
          project.Details.PreferentialFeeConditions =
              new MarkdownString(request.PreferentialFeeConditions);

          await UnitOfWork.SaveChangesAsync();
      }

      public async Task MarkPreferential(MarkPreferentialRequest request)
      {
          var claim = await LoadClaimAsMaster(request, acl => acl.CanManageMoney);

          claim.PreferentialFeeUser = request.Preferential;
          await UnitOfWork.SaveChangesAsync();
      }

      public async Task RequestPreferentialFee(MarkMeAsPreferentialFeeOperationRequest request)
      {
          var claim = await LoadClaimAsMaster(request, ExtraAccessReason.Player);

          CheckOperationDate(request.OperationDate);

          var comment = AddCommentImpl(claim,
              null,
              request.Contents,
              isVisibleToPlayer: true,
              extraAction: CommentExtraAction.RequestPreferential);

          var financeOperation = new FinanceOperation()
          {
              Created = Now,
              FeeChange = 0,
              MoneyAmount = 0,
              Changed = Now,
              Claim = claim,
              Comment = comment,
              PaymentType = null,
              State = FinanceOperationState.Proposed,
              ProjectId = claim.ProjectId,
              OperationDate = request.OperationDate,
              MarkMeAsPreferential = true,
          };

          comment.Finance = financeOperation;

          claim.FinanceOperations.Add(financeOperation);

          claim.UpdateClaimFeeIfRequired(request.OperationDate);

            await UnitOfWork.SaveChangesAsync();

          var email = await CreateClaimEmail<FinanceOperationEmail>(claim, request.Contents,
              s => s.MoneyOperation,
              commentExtraAction: CommentExtraAction.RequestPreferential);


            await EmailService.Email(email);
        }
  }
}
