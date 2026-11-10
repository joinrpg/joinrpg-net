namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Цена — decimal: копейки показываются только если они есть («1500», «49,9»),
/// целые рубли без дробной части. Разделитель — запятая (русская локаль).
/// </summary>
public class PriceElementTest
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(1500, "1500")]
    [InlineData(-200, "-200")]
    [InlineData(49.90, "49,9")]
    [InlineData(0.05, "0,05")]
    public void RendersKopecksOnlyWhenPresent(decimal price, string expected)
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<PriceElement>(p => p.Add(x => x.Price, price));

        cut.Find("span.price-value").TextContent.Trim().ShouldBe(expected);
    }
}
