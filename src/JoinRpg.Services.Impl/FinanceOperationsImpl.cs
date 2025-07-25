using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl;

public class FinanceOperationsImpl : ClaimImplBase, IFinanceService
{

    private IVirtualUsersService _vpu;

    public FinanceOperationsImpl(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IVirtualUsersService vpu,
        ICurrentUserAccessor currentUserAccessor,
        IProjectMetadataRepository projectMetadataRepository) : base(unitOfWork, emailService, currentUserAccessor, projectMetadataRepository) => _vpu = vpu;

    public async Task FeeAcceptedOperation(FeeAcceptedOperationRequest request)
    {
        var (claim, _) = await LoadClaimAsMaster(request, Permission.None, ExtraAccessReason.Player);
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

    #region Payment type

    /// <inheritdoc />
    public async Task CreatePaymentType(CreatePaymentTypeRequest request)
    {
        // Loading project
        var project = await ProjectRepository.GetProjectForFinanceSetup(request.ProjectId);

        // Checking access rights of the current user
        if (!IsCurrentUserAdmin)
        {
            _ = project.RequestMasterAccess(CurrentUserId, Permission.CanManageMoney);
        }

        // Preparing master Id and checking if the same payment type already created
        int masterId;
        if (!request.TypeKind.IsOnline())
        {
            _ = project.RequestMasterAccess(request.TargetMasterId);

            // Cash payment could be only one
            if (request.TypeKind == PaymentTypeKind.Cash
                && project.PaymentTypes.Any(pt => pt.UserId == request.TargetMasterId && pt.TypeKind == PaymentTypeKind.Cash))
            {
                throw new JoinRpgInvalidUserException($@"Payment of type ${request.TypeKind.GetDisplayName()} is already created for the user ${request.TargetMasterId}");
            }

            masterId = request.TargetMasterId.Value;
        }
        else
        {
            if (project.PaymentTypes.Any(pt => pt.TypeKind == request.TypeKind))
            {
                throw new DataException($"Can't create more than one {request.TypeKind} payment type");
            }

            masterId = _vpu.PaymentsUser.UserId;
        }

        // Creating payment type
        var result = new PaymentType(request.TypeKind, request.ProjectId, masterId);

        // Configuring payment type
        if (result.TypeKind == PaymentTypeKind.Custom)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);
            // Checking custom payment type name
            result.Name = request.Name.Trim();
        }

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
                {
                    _ = project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);
                }

                break;
            case PaymentTypeKind.Online:
            case PaymentTypeKind.OnlineSubscription:
                if (!IsCurrentUserAdmin)
                {
                    // Regular master with finance management permissions can disable online payments
                    if (paymentType.IsActive)
                    {
                        _ = project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);
                    }
                    // ...but to enable them back he must have admin permissions
                    else
                    {
                        throw new MustBeAdminException();
                    }
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(paymentType.TypeKind), paymentType.TypeKind, null);
        }

        if (paymentType.IsActive)
        {
            _ = SmartDelete(paymentType);
        }
        else
        {
            paymentType.IsActive = true;
        }

        await UnitOfWork.SaveChangesAsync();
    }

    public async Task EditCustomPaymentType(int projectId,
        int paymentTypeId,
        string name,
        bool isDefault)
    {
        var project = await ProjectRepository.GetProjectForFinanceSetup(projectId);
        _ = project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);

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

    #endregion

    #region Fee options

    public async Task CreateFeeSetting(CreateFeeSettingRequest request)
    {
        var project = await ProjectRepository.GetProjectForFinanceSetup(request.ProjectId);
        _ = project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);

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
        _ = project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);

        var feeSetting =
            project.ProjectFeeSettings.Single(pt =>
                pt.ProjectFeeSettingId == projectFeeSettingId);

        if (feeSetting.StartDate < DateTime.UtcNow)
        {
            throw new CannotPerformOperationInPast();
        }

        _ = UnitOfWork.GetDbSet<ProjectFeeSetting>().Remove(feeSetting);

        await UnitOfWork.SaveChangesAsync();
    }

    public async Task ChangeFee(int projectId, int claimId, int feeValue)
    {
        var (claim, _) = await LoadClaimAsMaster(new(projectId), claimId, Permission.CanManageMoney);

        _ = AddCommentImpl(claim,
            null,
            feeValue.ToString(),
            isVisibleToPlayer: true,
            extraAction: CommentExtraAction.FeeChanged);

        claim.CurrentFee = feeValue;

        await UnitOfWork.SaveChangesAsync();
    }

    #endregion

    #region Finance settings

    public async Task SaveGlobalSettings(SetFinanceSettingsRequest request)
    {
        var project = await ProjectRepository.GetProjectForFinanceSetup(request.ProjectId);
        _ = project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);

        project.Details.FinanceWarnOnOverPayment = request.WarnOnOverPayment;
        project.Details.PreferentialFeeEnabled = request.PreferentialFeeEnabled;
        project.Details.PreferentialFeeConditions =
            new MarkdownString(request.PreferentialFeeConditions);

        await UnitOfWork.SaveChangesAsync();
    }

    #endregion

    #region Finance Operations

    public async Task MarkPreferential(MarkPreferentialRequest request)
    {
        var (claim, _) = await LoadClaimAsMaster(request, Permission.CanManageMoney);

        claim.PreferentialFeeUser = request.Preferential;
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task RequestPreferentialFee(MarkMeAsPreferentialFeeOperationRequest request)
    {
        var (claim, projectInfo) = await LoadClaimAsMaster(request, Permission.None, ExtraAccessReason.Player);

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

        claim.UpdateClaimFeeIfRequired(request.OperationDate, projectInfo);

        await UnitOfWork.SaveChangesAsync();

        var email = await CreateClaimEmail<FinanceOperationEmail>(claim,
            request.Contents,
            s => s.MoneyOperation,
            commentExtraAction: CommentExtraAction.RequestPreferential);


        await EmailService.Email(email);
    }

    private async Task<Tuple<Comment, Comment>> AddTransferCommentsAsync(
        Claim claimFrom,
        Claim claimTo,
        ClaimPaymentTransferRequest request)
    {
        // Comment to source claim
        Comment commentFrom = CommentHelper.CreateCommentForClaim(
            claimFrom,
            CurrentUserId,
            Now,
            request.CommentText,
            true,
            null);
        commentFrom.Finance = new FinanceOperation
        {
            OperationType = FinanceOperationType.TransferTo,
            MoneyAmount = -request.Money,
            OperationDate = request.OperationDate,
            ProjectId = request.ProjectId,
            ClaimId = request.ClaimId,
            LinkedClaimId = request.ToClaimId,
            Created = Now,
            Changed = Now,
            State = FinanceOperationState.Approved,
        };
        _ = UnitOfWork.GetDbSet<Comment>().Add(commentFrom);

        // Comment to destination claim
        Comment commentTo = CommentHelper.CreateCommentForClaim(
            claimTo,
            CurrentUserId,
            Now,
            request.CommentText,
            true,
            null);
        commentTo.Finance = new FinanceOperation
        {
            OperationType = FinanceOperationType.TransferFrom,
            MoneyAmount = request.Money,
            OperationDate = request.OperationDate,
            ProjectId = request.ProjectId,
            ClaimId = request.ToClaimId,
            LinkedClaimId = request.ClaimId,
            Created = Now,
            Changed = Now,
            State = FinanceOperationState.Approved,
        };

        await UnitOfWork.SaveChangesAsync();

        return Tuple.Create(commentFrom, commentTo);
    }

    /// <inheritdoc />
    public async Task TransferPaymentAsync(ClaimPaymentTransferRequest request)
    {
        // Loading source claim
        var (claimFrom, projectInfo) = await LoadClaimAsMaster(request, Permission.CanManageMoney);

        // Loading destination claim
        var (claimTo, _) = await LoadClaimAsMaster(new(request.ProjectId), request.ToClaimId);

        // Checking money amount
        var availableMoney = claimFrom.GetPaymentSum();
        if (availableMoney < request.Money)
        {
            throw new PaymentException(claimFrom.Project, $"Not enough money at claim {claimFrom.Character.CharacterName} to perform transfer");
        }

        // Adding comments
        _ = await AddTransferCommentsAsync(
            claimFrom,
            claimTo,
            request);

        // Trying to fix fee in destination claim
        claimTo.UpdateClaimFeeIfRequired(Now, projectInfo);
        await UnitOfWork.SaveChangesAsync();
    }

    #endregion

    #region Master money management

    public async Task CreateTransfer(CreateTransferRequest request)
    {
        var project = await ProjectRepository.GetProjectForFinanceSetup(request.ProjectId);

        _ = project.RequestMasterAccess(CurrentUserId);
        _ = project.RequestMasterAccess(request.Sender);
        _ = project.RequestMasterAccess(request.Receiver);

        if (request.Sender == request.Receiver)
        {
            throw new DbEntityValidationException();
        }

        if (request.Sender != CurrentUserId && request.Receiver != CurrentUserId)
        {
            _ = project.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);
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
        }
        else if (CurrentUserId == request.Receiver)
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
        _ = moneyTransfer.RequestMasterAccess(CurrentUserId);

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
                _ = moneyTransfer.RequestMasterAccess(CurrentUserId, acl => acl.CanManageMoney);
                moneyTransfer.ResultState = request.Approved
                    ? MoneyTransferState.Approved
                    : MoneyTransferState.Declined;
                break;
        }

        moneyTransfer.ChangedById = CurrentUserId;
        moneyTransfer.Changed = DateTimeOffset.UtcNow;

        await UnitOfWork.SaveChangesAsync();
    }

    #endregion
}
