using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Common.WebComponents.Test;

public class EnumSelectorTest
{
    public enum Fruit
    {
        [Display(Name = "Яблоко")]
        Apple,
        [Display(Name = "Груша")]
        Pear,
    }

    // Без отметки selected браузер показывает первый пункт — и выбранное не первым значение выглядит сброшенным.
    [Fact]
    public void SelectedValue_IsMarkedSelected()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<EnumSelector<Fruit>>(p => p.Add(x => x.SelectedValue, Fruit.Pear));

        cut.FindAll("option").Where(o => o.HasAttribute("selected")).Select(o => o.TextContent)
            .ShouldBe(["Груша"]);
    }
}
