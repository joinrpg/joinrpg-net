using JoinRpg.DomainTypes.Characters.Claims.Finances;

namespace JoinRpg.DomainTypes.Test.Characters;

public class ClaimBalanceTest
{
    [Theory]
    [InlineData(1000, 1500, ClaimPaymentStatus.Overpaid, true)]
    [InlineData(1000, 1000, ClaimPaymentStatus.Paid, true)]
    [InlineData(1000, 300, ClaimPaymentStatus.MoreToPay, false)]
    [InlineData(1000, 0, ClaimPaymentStatus.NotPaid, false)]
    // Взноса нет и ничего не уплачено — оплачено, а не «не оплачено».
    [InlineData(0, 0, ClaimPaymentStatus.Paid, true)]
    // После возврата уплаченное может уйти в минус.
    [InlineData(1000, -100, ClaimPaymentStatus.NotPaid, false)]
    public void PaymentStatus(int totalFee, int feePaid, ClaimPaymentStatus expected, bool isPaid)
    {
        var balance = new ClaimBalance(FeePaid: feePaid, TotalFee: totalFee);

        balance.PaymentStatus.ShouldBe(expected);
        balance.IsPaid.ShouldBe(isPaid);
    }
}
