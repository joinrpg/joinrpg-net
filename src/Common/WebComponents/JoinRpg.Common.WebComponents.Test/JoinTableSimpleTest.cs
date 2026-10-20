namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// <see cref="JoinTableSimple.Responsive"/>: по умолчанию таблица лежит в прокручиваемой обёртке,
/// и её можно отключить — например, ради выпадающих меню в ячейках.
/// </summary>
public class JoinTableSimpleTest
{
    [Fact]
    public void Default_TableIsInsideScrollWrapper()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<JoinTableSimple>();

        cut.Find(".join-table-responsive > table").ShouldNotBeNull();
    }

    [Fact]
    public void ResponsiveOff_TableHasNoWrapper()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<JoinTableSimple>(p => p.Add(x => x.Responsive, false));

        cut.FindAll(".join-table-responsive").ShouldBeEmpty();
        cut.Find("table").ShouldNotBeNull();
    }
}
