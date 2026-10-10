using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;

namespace JoinRpg.Domain.Test;

/// <summary>
/// Какими способами пользователь может отметить оплату по заявке:
/// <see cref="ProjectInfo.GetAvailablePaymentTypesForUser"/>.
/// </summary>
public class AvailablePaymentTypesTest
{
    private readonly MockedProject mock = new();
    private readonly User masterWithoutMoney;
    private readonly int financeMasterTypeId;
    private readonly int masterWithoutMoneyTypeId;

    public AvailablePaymentTypesTest()
    {
        masterWithoutMoney = mock.CreateMaster();
        mock.Project.ProjectAcls.Single(acl => acl.UserId == masterWithoutMoney.UserId).CanManageMoney = false;

        financeMasterTypeId = mock.CreateCashPaymentType().PaymentTypeId;
        masterWithoutMoneyTypeId = mock.CreateCashPaymentType(masterWithoutMoney).PaymentTypeId;
        mock.CreateCashPaymentType(mock.CreateMaster("Master3")).IsActive = false;
        mock.ReInitProjectInfo();
    }

    private IEnumerable<int> AvailableFor(User user)
        => mock.ProjectInfo
            .GetAvailablePaymentTypesForUser(new UserIdentification(user.UserId), new UserIdentification(mock.Player.UserId))
            .Select(pt => pt.PaymentTypeId.PaymentTypeId);

    [Fact]
    public void PlayerGetsAllEnabledTypes()
        => AvailableFor(mock.Player).ShouldBe([financeMasterTypeId, masterWithoutMoneyTypeId], ignoreOrder: true);

    [Fact]
    public void FinanceMasterGetsAllEnabledTypes()
        => AvailableFor(mock.Master).ShouldBe([financeMasterTypeId, masterWithoutMoneyTypeId], ignoreOrder: true);

    [Fact]
    public void MasterWithoutMoneyAccessGetsOnlyOwnTypes()
        => AvailableFor(masterWithoutMoney).ShouldBe([masterWithoutMoneyTypeId]);
}
