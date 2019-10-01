using System;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Email;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl
{

    [UsedImplicitly]
    public class FinanceOperationsImpl : ClaimImplBase, IFinanceService
    {

        private IVirtualUsersService _vpu;

        public FinanceOperationsImpl(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IFieldDefaultValueGenerator fieldDefaultValueGenerator,
            IVirtualUsersService vpu
            ) : base(unitOfWork, emailService, fieldDefaultValueGenerator)
        {
            _vpu = vpu;
        }

        public async Task FeeAcceptedOperation(FeeAcceptedOperationRequest request)
        {
            var claim = await LoadClaimAsMaster(request, ExtraAccessReason.Player);
            var paymentType =
                claim.Project.PaymentTypes.Single(pt => pt.PaymentTypeId == request.PaymentTypeId);

            var email = await AcceptFeeImpl(request.Contents,
                request.OperationDate,
                request.FeeChange,
                request.Money,
                paymentType,
                claim);

            await UnitOfWork.SaveChangesAsync();

            await EmailService.Email(email);
        }

        /// <inheritdoc />
        public async Task CreatePaymentType(CreatePaymentTypeRequest request)
        {
            // Loading project
            var project = await ProjectRepository.GetProjectForFinanceSetup(request.ProjectId);

            // Checking access rights of the current user
            if (!IsCurrentUserAdmin)
                project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);

            // Preparing master Id and checking if the same payment type already created
            int masterId;
            if (request.TypeKind != PaymentTypeKind.Online)
            {
                if (request.TargetMasterId == null)
                    throw new ArgumentNullException(nameof(request.TargetMasterId), @"Target master must be specified");
                project.RequestMasterAccess(request.TargetMasterId);
                // Cash payment could be only one
                if (request.TypeKind == PaymentTypeKind.Cash && project.PaymentTypes.FirstOrDefault(pt => pt.UserId == request.TargetMasterId) != null)
                    throw new JoinRpgInvalidUserException($@"Payment of type ${request.TypeKind.GetDisplayName()} is already created for the user ${request.TargetMasterId}");
                masterId = request.TargetMasterId.Value;
            }
            else
            {
                if (project.PaymentTypes.Any(pt => pt.TypeKind == PaymentTypeKind.Online))
                    throw new DataException("Can't create more than one online payment type");
                masterId = _vpu.PaymentsUser.UserId;
            }

            // Checking custom payment type name
            if (request.TypeKind == PaymentTypeKind.Custom && string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentNullException(nameof(request.Name), "Custom payment type name must be specified");

            // Creating payment type
            var result = new PaymentType(request.TypeKind, request.ProjectId, masterId);

            // Configuring payment type
            if (result.TypeKind == PaymentTypeKind.Custom)
                result.Name = request.Name.Trim();

            // Saving
            project.PaymentTypes.Add(result);
            await UnitOfWork.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task TogglePaymentActiveness(int projectId, int paymentTypeId)
        {
            var project = await ProjectRepository.GetProjectForFinanceSetup(projectId);
            var paymentType = project.PaymentTypes.Single(pt => pt.PaymentTypeId == paymentTypeId);

            switch (paymentType.TypeKind)
            {
                case PaymentTypeKind.Custom:
                case PaymentTypeKind.Cash:
                    if (!IsCurrentUserAdmin)
                        project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);
                    break;
                case PaymentTypeKind.Online:
                    if (!IsCurrentUserAdmin)
                    {
                        // Regular master with finance management permissions can disable online payments
                        if (paymentType.IsActive)
                            project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);
                        // ...but to enable them back he must have admin permissions
                        else 
                            throw new MustBeAdminException();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(paymentType.TypeKind), paymentType.TypeKind, null);
            }

            if (paymentType.IsActive)
                SmartDelete(paymentType);
            else
                paymentType.IsActive = true;

            await UnitOfWork.SaveChangesAsync();
        }

        public async Task EditCustomPaymentType(int projectId,
            int paymentTypeId,
            string name,
            bool isDefault)
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

            var feeSetting =
                project.ProjectFeeSettings.Single(pt =>
                    pt.ProjectFeeSettingId == projectFeeSettingId);

            if (feeSetting.StartDate < DateTime.UtcNow)
            {
                throw new CannotPerformOperationInPast();
            }

            UnitOfWork.GetDbSet<ProjectFeeSetting>().Remove(feeSetting);

            await UnitOfWork.SaveChangesAsync();
        }

        public async Task ChangeFee(int projectId, int claimId, int feeValue)
        {
            var claim = (await ClaimsRepository.GetClaim(projectId, claimId))

                .RequestAccess(CurrentUserId, acl => acl.CanManageMoney);

            AddCommentImpl(claim,
                null,
                feeValue.ToString(),
                isVisibleToPlayer: true,
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
                OperationType = FinanceOperationType.PreferentialFeeRequest,
            };

            comment.Finance = financeOperation;

            claim.FinanceOperations.Add(financeOperation);

            claim.UpdateClaimFeeIfRequired(request.OperationDate);

            await UnitOfWork.SaveChangesAsync();

            var email = await CreateClaimEmail<FinanceOperationEmail>(claim,
                request.Contents,
                s => s.MoneyOperation,
                commentExtraAction: CommentExtraAction.RequestPreferential);


            await EmailService.Email(email);
        }

        public async Task CreateTransfer(CreateTransferRequest request)
        {
            var project = await ProjectRepository.GetProjectForFinanceSetup(request.ProjectId);

            project.RequestMasterAccess(CurrentUserId);
            project.RequestMasterAccess(request.Sender);
            project.RequestMasterAccess(request.Receiver);

            if (request.Sender == request.Receiver)
            {
                throw new DbEntityValidationException();
            }

            if (request.Sender != CurrentUserId && request.Receiver != CurrentUserId)
            {
                project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);
            }

            CheckOperationDate(request.OperationDate);

            if (request.Amount <= 0)
            {
                throw new DbEntityValidationException();
            }

            var transfer = new MoneyTransfer()
            {
                SenderId = request.Sender,
                Amount = request.Amount,
                Changed = DateTimeOffset.UtcNow,
                Created = DateTimeOffset.UtcNow,
                ChangedById = CurrentUserId,
                CreatedById = CurrentUserId,
                OperationDate = request.OperationDate,
                ProjectId = request.ProjectId,
                ReceiverId = request.Receiver,
                TransferText = new TransferText()
                {
                    Text = new MarkdownString(Required(request.Comment)),
                },
            };

            if (CurrentUserId == request.Sender)
            {
                transfer.ResultState = MoneyTransferState.PendingForReceiver;
            } else if (CurrentUserId == request.Receiver)
            {
                transfer.ResultState = MoneyTransferState.PendingForSender;
            }
            else
            {
                transfer.ResultState = MoneyTransferState.PendingForBoth;
            }

            project.MoneyTransfers.Add(transfer);

            //TODO send email

            await UnitOfWork.SaveChangesAsync();
        }

        public async Task MarkTransfer(ApproveRejectTransferRequest request)
        {
            var moneyTransfer = await UnitOfWork.GetDbSet<MoneyTransfer>()
                .Include(transfer => transfer.Project)
                .Include(transfer => transfer.Sender)
                .Include(transfer => transfer.Receiver)
                .SingleAsync(transfer => transfer.Id == request.MoneyTranferId &&
                                    transfer.ProjectId == request.ProjectId);
            moneyTransfer.RequestMasterAccess(CurrentUserId);

            switch (moneyTransfer.ResultState)
            {
                case MoneyTransferState.Approved:
                case MoneyTransferState.Declined:
                    throw new EntityWrongStatusException(moneyTransfer);

                case MoneyTransferState.PendingForReceiver when CurrentUserId == moneyTransfer.ReceiverId:
                case MoneyTransferState.PendingForSender when CurrentUserId == moneyTransfer.SenderId:
                    moneyTransfer.ResultState = request.Approved
                        ? MoneyTransferState.Approved
                        : MoneyTransferState.Declined;
                    break;

                case MoneyTransferState.PendingForBoth when CurrentUserId == moneyTransfer.ReceiverId:
                    moneyTransfer.ResultState = request.Approved
                        ? MoneyTransferState.PendingForSender
                        : MoneyTransferState.Declined;
                    break;
                case MoneyTransferState.PendingForBoth when CurrentUserId == moneyTransfer.SenderId:
                    moneyTransfer.ResultState = request.Approved
                        ? MoneyTransferState.PendingForReceiver
                        : MoneyTransferState.Declined;
                    break;

                default: //admin tries to approve with superpowers
                    moneyTransfer.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);
                    moneyTransfer.ResultState = request.Approved
                        ? MoneyTransferState.Approved
                        : MoneyTransferState.Declined;
                    break;
            }

            moneyTransfer.ChangedById = CurrentUserId;
            moneyTransfer.Changed = DateTimeOffset.UtcNow;

            await UnitOfWork.SaveChangesAsync();
        }
    }
}
