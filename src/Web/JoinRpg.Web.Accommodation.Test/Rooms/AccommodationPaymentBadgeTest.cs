using JoinRpg.Web.Accommodation.Rooms;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Accommodation.Test.Rooms;

/// <summary>
/// Цвет бейджа оплаты считается по доле оплаченного, а не по доле оставшегося долга.
/// </summary>
public class AccommodationPaymentBadgeTest
{
    [Theory]
    [InlineData(1000, 1000, "danger")] // ничего не оплачено
    [InlineData(900, 1000, "danger")] // оплачено 10%
    [InlineData(500, 1000, "warning")] // оплачено 50%
    [InlineData(100, 1000, "warning")] // оплачено 90%
    [InlineData(0, 1000, "success")] // всё оплачено
    [InlineData(0, 0, "success")] // бесплатно
    public void ColorDependsOnPaidShare(int feeToPay, int feeTotal, string expected)
        => AccommodationPaymentBadge.GetCssClass(feeToPay, feeTotal).ShouldBe(expected);
}
