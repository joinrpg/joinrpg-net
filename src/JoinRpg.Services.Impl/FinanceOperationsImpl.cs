using System.Data.Entity;
using System.Data.Entity.Validation;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Impl.Characters;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl;

internal class FinanceOperationsImpl(
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    ICurrentUserAccessor currentUserAccessor,
    IClaimNotificationService claimNotificationService,
    CommentHelper commentHelper,
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterPropsService characterPropsService) : ClaimImplBase(unitOfWork, emailService, currentUserAccessor, projectMetadataRepository, commentHelper), IFinanceService
{
    public async Task FeeAcceptedOperation(FeeAcceptedOperationRequest request)
    {
        var (claim, projectInfo) = await LoadClaimAsMaster(request, Permission.None, ExtraAccessReason.Player);


        var (comment, email) = AcceptFeeImpl(request.Contents,
            request.OperationDate,
            request.Money,
            projectInfo.ProjectFinanceSettings.GetRequiredPayment(request.PaymentTypeId),
            claim,
            projectInfo
            );

        await UnitOfWork.SaveChangesAsync();

        await claimNotificationService.SendNotification(email.WithCommentId(comment.CommentId));
    }

    #region Fee

    public Task ChangeFee(ClaimIdentification claimId, int feeValue)
        => characterPropsService.ChangeClaim(
            claimId,
            ClaimAccessRequirement.ManageMoney,
            ProjectActiveRequirement.MustBeActive,
            feeValue,
            ctx =>
            {
                _ = ctx.AddComment(
                    ctx.Request.ToString(),
                    CommentExtraAction.FeeChanged,
                    ClaimOperationType.MasterVisibleChange);

                ctx.Claim.CurrentFee = ctx.Request;
            });

    #endregion


    #region Finance Operations

    public Task MarkPreferential(MarkPreferentialRequest request)
        => characterPropsService.ChangeClaim(
            new ClaimIdentification(request.ProjectId, request.ClaimId),
            ClaimAccessRequirement.ManageMoney,
            ProjectActiveRequirement.MustBeActive,
            request,
            ctx => ctx.Claim.PreferentialFeeUser = ctx.Request.Preferential);

    public Task RequestPreferentialFee(MarkMeAsPreferentialFeeOperationRequest request)
        => characterPropsService.ChangeClaim(
            new ClaimIdentification(request.ProjectId, request.ClaimId),
            ClaimAccessRequirement.MasterOrPlayer,
            ProjectActiveRequirement.MustBeActive,
            request,
            ctx =>
            {
                CheckOperationDate(ctx.Request.OperationDate, ctx.Now);

                var pending = ctx.AddComment(
                    ctx.Request.Contents,
                    CommentExtraAction.RequestPreferential,
                    ClaimOperationType.PlayerChange);

                var financeOperation = new FinanceOperation()
                {
                    Created = ctx.Now,
                    MoneyAmount = 0,
                    Changed = ctx.Now,
                    Claim = ctx.Claim,
                    Comment = pending.Comment,
                    PaymentType = null,
                    State = FinanceOperationState.Proposed,
                    ProjectId = ctx.Claim.ProjectId,
                    OperationDate = ctx.Request.OperationDate,
                    OperationType = FinanceOperationType.PreferentialFeeRequest,
                };

                pending.Comment.Finance = financeOperation;

                ctx.Claim.FinanceOperations.Add(financeOperation);

                ctx.Claim.UpdateClaimFeeIfRequired(ctx.Request.OperationDate, ctx.ProjectInfo);
            });

    /// <inheritdoc />
    public async Task TransferPaymentAsync(ClaimPaymentTransferRequest request)
    {
        // Loading source claim
        var (claimFrom, projectInfo) = await LoadClaimAsMaster(request, Permission.CanManageMoney);

        // Loading destination claim
        var (claimTo, _) = await LoadClaimAsMaster(new ClaimIdentification(request.ProjectId, request.ToClaimId));

        // Checking money amount
        var availableMoney = claimFrom.GetPaymentSum();
        if (availableMoney < request.Money)
        {
            throw new PaymentException(claimFrom.Project, $"Not enough money at claim {claimFrom.Character.CharacterName} to perform transfer");
        }

        // Comment to source claim
        var (commentFrom, emailFrom) = CommentHelper.CreateClaimCommentWithNotification(
            request.CommentText ?? "",
            claimFrom,
            projectInfo,
            CommentExtraAction.TransferFrom,
            ClaimOperationType.MasterVisibleChange,
            Now
            );

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

        // Comment to destination claim
        var (commentTo, emailTo) = CommentHelper.CreateClaimCommentWithNotification(
            request.CommentText ?? "",
            claimTo,
            projectInfo,
            CommentExtraAction.TransferTo,
            ClaimOperationType.MasterVisibleChange,
            Now
            );
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

        // Trying to fix fee in destination claim
        claimTo.UpdateClaimFeeIfRequired(Now, projectInfo);

        await UnitOfWork.SaveChangesAsync();

        await claimNotificationService.SendNotification(emailTo.WithCommentId(commentTo.CommentId));
        await claimNotificationService.SendNotification(emailFrom.WithCommentId(commentFrom.CommentId));
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
            _ = project.RequestMasterAccess(CurrentUserId, Permission.CanManageMoney);
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
                Text = new MarkdownDbValue(Required(request.Comment)),
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
                _ = moneyTransfer.RequestMasterAccess(CurrentUserId, Permission.CanManageMoney);
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
