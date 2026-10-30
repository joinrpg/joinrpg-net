using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>
/// Когда в заявке виден блок взноса — а с ним и кнопки «Отметить оплату…» и «Изменить взнос».
/// </summary>
public class ClaimFeeViewModelTest
{
    private readonly MockedProject mock = new();

    private ClaimFeeViewModel Build(Claim claim, User? currentUser = null)
    {
        var claimInCharacter = new ClaimInCharacter(mock.GetCharacterInfo(claim.Character), claim.GetId());
        return new ClaimFeeViewModel(
            claim,
            claimInCharacter,
            new UserIdentification((currentUser ?? mock.Master).UserId),
            _ => null,
            accommodation: null,
            preferentialFeeConditions: null);
    }

    [Fact]
    public void NoFee_PaidAccommodationNotSelected_FeeShown()
    {
        // #5646: взноса в проекте нет, поселение платное, а проживание в заявке не выбрано —
        // мастер всё равно должен иметь возможность отметить оплату.
        mock.Project.Details.EnableAccommodation = true;
        _ = mock.CreateAccommodationType("Домик", cost: 1000);
        mock.ReInitProjectInfo();
        var claim = mock.CreateApprovedClaim(mock.CreateCharacter("Бездомный"), mock.Player);

        var model = Build(claim);

        model.AccommodationFee.ShouldBe(0);
        model.ShowFee.ShouldBeTrue();
    }

    [Fact]
    public void NoFee_AccommodationDisabled_FeeHidden()
    {
        _ = mock.CreateAccommodationType("Домик", cost: 1000);
        mock.ReInitProjectInfo();
        var claim = mock.CreateApprovedClaim(mock.CreateCharacter("Бесплатный"), mock.Player);

        Build(claim).ShowFee.ShouldBeFalse();
    }

    [Fact]
    public void NoFee_OnlyFreeAccommodation_FeeHidden()
    {
        mock.Project.Details.EnableAccommodation = true;
        _ = mock.CreateAccommodationType("Своя палатка", cost: 0);
        mock.ReInitProjectInfo();
        var claim = mock.CreateApprovedClaim(mock.CreateCharacter("Турист"), mock.Player);

        Build(claim).ShowFee.ShouldBeFalse();
    }

    /// <summary>
    /// Операция игрока на кассу <paramref name="cashOwner"/> в состоянии <paramref name="state"/>.
    /// </summary>
    private FinanceOperation AddPayment(Claim claim, User cashOwner, FinanceOperationState state)
    {
        var cash = mock.CreateCashPaymentType(cashOwner);
        var operation = new FinanceOperation
        {
            CommentId = 1000 + claim.FinanceOperations.Count,
            Claim = claim,
            ClaimId = claim.ClaimId,
            ProjectId = mock.Project.ProjectId,
            OperationType = FinanceOperationType.Submit,
            State = state,
            MoneyAmount = 500,
            OperationDate = DateTime.UtcNow,
            PaymentTypeId = cash.PaymentTypeId,
            PaymentType = cash,
        };
        claim.FinanceOperations.Add(operation);
        return operation;
    }

    private static FinanceOperationViewModel Row(ClaimFeeViewModel model, FinanceOperation operation)
        => model.FinanceOperations.Single(fo => fo.Id == operation.CommentId);

    [Fact]
    public void ProposedPayment_MoneyMaster_GetsModerationButton()
    {
        var claim = mock.CreateApprovedClaim(mock.CreateCharacter("Платящий"), mock.Player);
        var operation = AddPayment(claim, mock.Master, FinanceOperationState.Proposed);

        Row(Build(claim), operation).FinanceModeration.ShouldNotBeNull()
            .OperationId.FinanceOperationId.ShouldBe(operation.CommentId);
    }

    [Fact]
    public void ApprovedPayment_HasNoModerationButton()
    {
        var claim = mock.CreateApprovedClaim(mock.CreateCharacter("Заплативший"), mock.Player);
        var operation = AddPayment(claim, mock.Master, FinanceOperationState.Approved);

        Row(Build(claim), operation).FinanceModeration.ShouldBeNull();
    }

    [Fact]
    public void ProposedPayment_PlayerAndForeignCashier_HaveNoModerationButton()
    {
        var cashier = mock.CreateMaster("Кассир");
        mock.Project.ProjectAcls.Single(acl => acl.UserId == cashier.UserId).CanManageMoney = false;
        var claim = mock.CreateApprovedClaim(mock.CreateCharacter("Платящий"), mock.Player);
        var operation = AddPayment(claim, mock.Master, FinanceOperationState.Proposed);

        Row(Build(claim, mock.Player), operation).FinanceModeration.ShouldBeNull();
        Row(Build(claim, cashier), operation).FinanceModeration.ShouldBeNull();
    }

    [Fact]
    public void HasModeratableFinanceOperations_OnlyForProposedPaymentThatUserCanModerate()
    {
        var claim = mock.CreateApprovedClaim(mock.CreateCharacter("Платящий"), mock.Player);
        _ = AddPayment(claim, mock.Master, FinanceOperationState.Approved);

        Build(claim).HasModeratableFinanceOperations.ShouldBeFalse();

        _ = AddPayment(claim, mock.Master, FinanceOperationState.Proposed);

        Build(claim).HasModeratableFinanceOperations.ShouldBeTrue();
        Build(claim, mock.Player).HasModeratableFinanceOperations.ShouldBeFalse();
    }
}
