using JoinRpg.DataModel.Mocks;

namespace JoinRpg.WebPortal.Models.Test;

public class PaymentTypeViewModelTest
{
    [Fact]
    public void CashTypeNameIncludesMaster()
    {
        var mock = new MockedProject();
        var paymentTypeId = mock.CreateCashPaymentType().PaymentTypeId;

        var paymentType = mock.ProjectInfo.ProjectFinanceSettings.PaymentTypes
            .Single(pt => pt.PaymentTypeId.PaymentTypeId == paymentTypeId);

        new PaymentTypeViewModel(paymentType).Name.ShouldContain("Master");
    }
}
