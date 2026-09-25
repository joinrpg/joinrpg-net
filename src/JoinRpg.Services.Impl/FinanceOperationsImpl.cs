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

namespace JoinRpg.Services.Impl;

internal class FinanceOperationsImpl(
    IUnitOfWork unitOfWork,
    ICurrentUserAccessor currentUserAccessor,
    IClaimNotificationService claimNotificationService,
    CommentHelper commentHelper,
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterPropsService characterPropsService) : DbServiceImplBase(unitOfWork, currentUserAccessor), IFinanceService
{
    public Task FeeAcceptedOperation(FeeAcceptedOperationRequest request)
        => characterPropsService.ChangeClaim(
            new ClaimIdentification(request.PaymentTypeId.ProjectId, request.ClaimId),
            ClaimAccessRequirement.MasterOrPlayer,
            ProjectActiveRequirement.MustBeActive,
            request,
            ctx => ctx.AcceptFee(
                ctx.Request.Contents,
                ctx.Request.OperationDate,
                ctx.Request.Money,
                ctx.ProjectInfo.ProjectFinanceSettings.GetRequiredPayment(ctx.Request.PaymentTypeId)));

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
                ctx.CheckOperationDate(ctx.Request.OperationDate);

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
    /// <remarks>
    /// <b>Намеренно не мигрирован на <c>ICharacterPropsService</c></b> (ADR014, список рисков):
    /// метод делает <b>два</b> <c>SaveChangesAsync</c>, и это не небрежность. Финоперация заявки-получателя
    /// создаётся с проставленным <c>ClaimId</c>, но не добавляется в <c>claimTo.FinanceOperations</c> —
    /// навигацию связывает relationship fixup при сохранении. Поэтому
    /// <see cref="FinanceExtensions.UpdateClaimFeeIfRequired"/> обязан считаться <b>после</b> первого
    /// сохранения, иначе новый платёж в баланс не попадёт и взнос зафиксируется неверно.
    /// <c>ChangeClaim</c> же даёт ровно одно сохранение, а мутируются здесь две заявки сразу.
    /// Перевод требует отдельного решения — см. отчёт по PR.
    /// </remarks>
    public async Task TransferPaymentAsync(ClaimPaymentTransferRequest request)
    {
        // Loading source claim
        var (claimFrom, projectInfo) = await LoadClaimAsMaster(
            new ClaimIdentification(request.ProjectId, request.ClaimId),
            Permission.CanManageMoney);

        // Loading destination claim
        var (claimTo, _) = await LoadClaimAsMaster(new ClaimIdentification(request.ProjectId, request.ToClaimId));

        // Checking money amount
        var availableMoney = claimFrom.GetPaymentSum();
        if (availableMoney < request.Money)
        {
            throw new PaymentException(claimFrom.Project, $"Not enough money at claim {claimFrom.Character.CharacterName} to perform transfer");
        }

        // Comment to source claim
        var (commentFrom, emailFrom) = commentHelper.CreateClaimCommentWithNotification(
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
        var (commentTo, emailTo) = commentHelper.CreateClaimCommentWithNotification(
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

    /// <summary>
    /// Последний остаток легаси-загрузки заявки: он же — последний потребитель
    /// <see cref="ClaimAcccessExtensions.RequestAccess"/> в этом сервисе. Приехал сюда из удалённого
    /// <c>ClaimImplBase</c> и жив ровно до тех пор, пока не мигрирован
    /// <see cref="TransferPaymentAsync"/>. Помечен <c>[Obsolete]</c> намеренно: предупреждение —
    /// burndown-метрика миграции (ADR014), гасить его надо переводом метода, а не pragma.
    /// </summary>
    [Obsolete("Используй ICharacterPropsService.ChangeClaim, см. ADR014")]
    private async Task<(Claim, ProjectInfo)> LoadClaimAsMaster(
        ClaimIdentification claimId,
        Permission permission = Permission.None)
    {
        var claim = await ClaimsRepository.GetClaim(claimId);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(claimId.ProjectId);

        return (claim.RequestAccess(CurrentUserId, permission), projectInfo);
    }

    #endregion

    #region Master money management

    // Обе операции этого региона работают с MoneyTransfer — переводом денег между мастерами. Ни
    // заявки, ни персонажа у них нет вовсе, поэтому ICharacterPropsService (ADR014) им не подходит
    // по определению: их агрегат — проект. Кандидат на IProjectPropsService (ADR009), но это
    // отдельное решение, а не часть миграции claim-контура.

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

        OperationDateValidation.CheckOperationDate(request.OperationDate, Now);

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
