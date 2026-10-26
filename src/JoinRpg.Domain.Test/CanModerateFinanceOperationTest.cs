using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Domain.Test;

public class CanModerateFinanceOperationTest
{
    private readonly MockedProject mock = new();

    /// <summary>Мастер без права на деньги, но со своим способом оплаты.</summary>
    private readonly User cashier;

    private readonly PaymentType cashierPaymentType;

    private readonly PaymentType moneyMasterPaymentType;

    public CanModerateFinanceOperationTest()
    {
        cashier = mock.CreateMaster("Кассир");
        mock.Project.ProjectAcls.Single(acl => acl.UserId == cashier.UserId).CanManageMoney = false;
        cashierPaymentType = mock.CreateCashPaymentType(cashier);
        moneyMasterPaymentType = mock.CreateCashPaymentType(mock.Master);
    }

    /// <summary>Способ оплаты операции; <c>null</c> — операция без способа оплаты (перевод, льгота).</summary>
    private PaymentTypeIdentification? Operation(PaymentType? paymentType)
        => paymentType is null ? null : new PaymentTypeIdentification(mock.ProjectInfo.ProjectId, paymentType.PaymentTypeId);

    private bool CanModerate(PaymentTypeIdentification? paymentTypeId, User? user)
        => mock.ProjectInfo.CanModerateFinanceOperation(paymentTypeId, user is null ? null : new UserIdentification(user.UserId));

    [Fact]
    public void MasterWithMoneyPermissionModeratesOtherPaymentType()
        => CanModerate(Operation(cashierPaymentType), mock.Master).ShouldBeTrue();

    [Fact]
    public void MasterWithMoneyPermissionModeratesOperationWithoutPaymentType()
        => CanModerate(Operation(null), mock.Master).ShouldBeTrue();

    [Fact]
    public void PaymentTypeOwnerModeratesOwnPaymentType()
        => CanModerate(Operation(cashierPaymentType), cashier).ShouldBeTrue();

    /// <summary>
    /// Отключённый способ оплаты остаётся в метаданных: уже поданные на него операции владелец подтверждает.
    /// </summary>
    [Fact]
    public void PaymentTypeOwnerModeratesOwnDisabledPaymentType()
    {
        cashierPaymentType.IsActive = false;
        mock.ReInitProjectInfo();

        CanModerate(Operation(cashierPaymentType), cashier).ShouldBeTrue();
    }

    [Fact]
    public void PaymentTypeOwnerDoesNotModerateOtherPaymentType()
        => CanModerate(Operation(moneyMasterPaymentType), cashier).ShouldBeFalse();

    [Fact]
    public void MasterWithoutMoneyPermissionDoesNotModerateOperationWithoutPaymentType()
        => CanModerate(Operation(null), cashier).ShouldBeFalse();

    [Fact]
    public void PaymentTypeOwnerWhoIsNoLongerMasterDoesNotModerate()
    {
        mock.Project.ProjectAcls.Remove(mock.Project.ProjectAcls.Single(acl => acl.UserId == cashier.UserId));
        mock.ReInitProjectInfo();

        CanModerate(Operation(cashierPaymentType), cashier).ShouldBeFalse();
    }

    [Fact]
    public void PlayerDoesNotModerate()
        => CanModerate(Operation(moneyMasterPaymentType), mock.Player).ShouldBeFalse();

    [Fact]
    public void AnonymousDoesNotModerate()
        => CanModerate(Operation(moneyMasterPaymentType), null).ShouldBeFalse();
}
