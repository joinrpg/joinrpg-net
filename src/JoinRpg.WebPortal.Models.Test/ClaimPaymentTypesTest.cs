using JoinRpg.DataModel.Mocks;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>
/// Способы оплаты на странице заявки строятся по метаданным проекта, а не по EF-навигации
/// <c>Project.ActivePaymentTypes</c>.
/// </summary>
public class ClaimPaymentTypesTest
{
    private readonly MockedProject mock = new();
    private readonly int masterTypeId;
    private readonly int otherMasterTypeId;

    public ClaimPaymentTypesTest()
    {
        masterTypeId = mock.CreateCashPaymentType().PaymentTypeId;
        otherMasterTypeId = mock.CreateCashPaymentType(mock.CreateMaster()).PaymentTypeId;
        var disabled = mock.CreateCashPaymentType(mock.CreateMaster("Master3"));
        disabled.IsActive = false;
        mock.ReInitProjectInfo();
    }

    [Fact]
    public void AnyPaymentTypeAllowedGetsAllEnabledTypes()
        => ClaimFeeViewModel.GetAvailablePaymentTypes(mock.ProjectInfo, new UserIdentification(mock.Player.UserId), canUseAnyPaymentType: true)
            .Select(pt => pt.PaymentTypeId)
            .ShouldBe([masterTypeId, otherMasterTypeId], ignoreOrder: true);

    [Fact]
    public void OtherMasterGetsOnlyOwnTypes()
        => ClaimFeeViewModel.GetAvailablePaymentTypes(mock.ProjectInfo, new UserIdentification(mock.Master.UserId), canUseAnyPaymentType: false)
            .Select(pt => pt.PaymentTypeId)
            .ShouldBe([masterTypeId]);

    [Fact]
    public void CashTypeNameIncludesMaster()
        => ClaimFeeViewModel.GetAvailablePaymentTypes(mock.ProjectInfo, new UserIdentification(mock.Master.UserId), canUseAnyPaymentType: false)
            .ShouldHaveSingleItem()
            .Name.ShouldContain("Master");
}
