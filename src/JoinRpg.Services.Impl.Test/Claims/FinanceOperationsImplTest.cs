using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.Test.Claims;

/// <summary>
/// Юнит-тесты финансовых операций над заявкой, переведённых на <c>ICharacterPropsService</c>
/// (ADR014). Проверяют и сам приём взноса, и <c>RequireFeeAccess</c> — escape-hatch, у которого
/// требуемое право зависит от аргументов операции.
/// </summary>
public class FinanceOperationsImplTest : ClaimServiceTestBase
{
    private FinanceOperationsImpl CreateService(int? currentUserId = null)
    {
        var currentUser = CreateCurrentUser(currentUserId);
        return new FinanceOperationsImpl(
            unitOfWork,
            emailService,
            currentUser,
            claimNotifications,
            new CommentHelper(currentUser),
            metadataRepository,
            CreatePropsService(currentUserId));
    }

    private Claim CreateClaim()
    {
        var character = mock.CreateCharacter("Вася");
        var claim = mock.CreateClaim(character, mock.Player);
        claim.ClaimStatus = ClaimStatus.Approved;
        mock.ReInitProjectInfo();
        return claim;
    }

    /// <summary>Мастер без права распоряжаться деньгами.</summary>
    private User CreateMasterWithoutMoneyRights()
    {
        var master = mock.CreateMaster("Безденежный");
        mock.Project.ProjectAcls.Single(acl => acl.UserId == master.UserId).CanManageMoney = false;
        mock.ReInitProjectInfo();
        return master;
    }

    private static FeeAcceptedOperationRequest Request(Claim claim, PaymentType paymentType, int money)
        => new()
        {
            ClaimId = claim.ClaimId,
            Contents = "получено",
            Money = money,
            OperationDate = new DateTime(2026, 09, 01),
            PaymentTypeId = new PaymentTypeIdentification(
                new ProjectIdentification(paymentType.ProjectId), paymentType.PaymentTypeId),
        };

    #region FeeAcceptedOperation

    [Fact]
    public async Task FeeAcceptedOperation_CreatesFinanceOperationCommentAndNotification()
    {
        var claim = CreateClaim();
        var paymentType = mock.CreateCashPaymentType();

        await CreateService().FeeAcceptedOperation(Request(claim, paymentType, 1000));

        var financeOperation = claim.FinanceOperations.ShouldHaveSingleItem();
        financeOperation.MoneyAmount.ShouldBe(1000);
        financeOperation.State.ShouldBe(FinanceOperationState.Approved);
        financeOperation.OperationType.ShouldBe(FinanceOperationType.Submit);
        financeOperation.PaymentTypeId.ShouldBe(paymentType.PaymentTypeId);

        var comment = claim.CommentDiscussion.Comments.ShouldHaveSingleItem();
        comment.Finance.ShouldBe(financeOperation);
        comment.ExtraAction.ShouldBe(CommentExtraAction.PaidFee);

        SaveChangesCallCount.ShouldBe(1);

        var notification = claimNotifications.SimpleChanged.ShouldHaveSingleItem();
        notification.Money.ShouldBe(1000);
        notification.PaymentOwner.ShouldNotBeNull().UserId.ShouldBe(new UserIdentification(mock.Master.UserId));
        notification.CommentExtraAction.ShouldBe(CommentExtraAction.PaidFee);
    }

    [Fact]
    public async Task FeeAcceptedOperation_Refund_CreatesRefundOperation()
    {
        var claim = CreateClaim();
        var paymentType = mock.CreateCashPaymentType();

        await CreateService().FeeAcceptedOperation(Request(claim, paymentType, -100));

        var financeOperation = claim.FinanceOperations.ShouldHaveSingleItem();
        financeOperation.OperationType.ShouldBe(FinanceOperationType.Refund);
        financeOperation.State.ShouldBe(FinanceOperationState.Approved);
        claimNotifications.SimpleChanged.ShouldHaveSingleItem().Money.ShouldBe(-100);
    }

    [Fact]
    public async Task FeeAcceptedOperation_InFuture_Throws()
    {
        var claim = CreateClaim();
        var paymentType = mock.CreateCashPaymentType();

        var request = Request(claim, paymentType, 1000);
        request.OperationDate = DateTime.UtcNow.AddDays(10);

        _ = await Should.ThrowAsync<CannotPerformOperationInFuture>(
            () => CreateService().FeeAcceptedOperation(request));

        SaveChangesCallCount.ShouldBe(0);
    }

    #endregion

    #region RequireFeeAccess

    [Fact]
    public async Task RequireFeeAccess_NegativeMoney_WithoutManageMoney_Throws()
    {
        var claim = CreateClaim();
        var paymentType = mock.CreateCashPaymentType();

        // Возврат делает только распорядитель денег — даже игроку своей же заявки он недоступен.
        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).FeeAcceptedOperation(Request(claim, paymentType, -100)));

        claim.FinanceOperations.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task RequireFeeAccess_PlayerPaysToForeignPaymentType_IsPlayerChange()
    {
        var claim = CreateClaim();
        var paymentType = mock.CreateCashPaymentType();

        await CreateService(mock.Player.UserId).FeeAcceptedOperation(Request(claim, paymentType, 1000));

        // Платёж заявлен игроком, но ещё не подтверждён мастером.
        claim.FinanceOperations.ShouldHaveSingleItem().State.ShouldBe(FinanceOperationState.Proposed);
        claimNotifications.SimpleChanged.ShouldHaveSingleItem()
            .ClaimOperationType.ShouldBe(ClaimOperationType.PlayerChange);
    }

    [Fact]
    public async Task RequireFeeAccess_MasterPaysToForeignPaymentType_WithoutManageMoney_Throws()
    {
        var claim = CreateClaim();
        var paymentType = mock.CreateCashPaymentType();
        var poorMaster = CreateMasterWithoutMoneyRights();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(poorMaster.UserId).FeeAcceptedOperation(Request(claim, paymentType, 1000)));

        claim.FinanceOperations.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task RequireFeeAccess_MasterPaysToOwnPaymentType_WithoutManageMoney_Allowed()
    {
        var claim = CreateClaim();
        var poorMaster = CreateMasterWithoutMoneyRights();
        var paymentType = mock.CreateCashPaymentType(poorMaster);

        await CreateService(poorMaster.UserId).FeeAcceptedOperation(Request(claim, paymentType, 1000));

        claim.FinanceOperations.ShouldHaveSingleItem().State.ShouldBe(FinanceOperationState.Approved);
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task RequireFeeAccess_DeactivatedPaymentType_Throws()
    {
        var claim = CreateClaim();
        var paymentType = mock.CreateCashPaymentType();
        paymentType.IsActive = false;
        mock.ReInitProjectInfo();

        _ = await Should.ThrowAsync<PaymentTypeInfoDeactivatedException>(
            () => CreateService().FeeAcceptedOperation(Request(claim, paymentType, 1000)));

        SaveChangesCallCount.ShouldBe(0);
    }

    #endregion
}
