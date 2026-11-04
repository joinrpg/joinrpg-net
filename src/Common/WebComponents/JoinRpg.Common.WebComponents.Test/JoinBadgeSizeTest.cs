namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// <see cref="JoinBadge.Size"/>: класс размера попадает на span бейджа. Раньше параметр
/// молча игнорировался — Bootstrap 3 не имеет размеров у .label, а своих классов в пакете
/// не было; регрессия ловится связкой этого теста с JoinBadge.razor.css.
/// </summary>
public class JoinBadgeSizeTest
{
    /// <summary>Кегль задаётся абсолютно (px), чтобы бейдж не менял размер от контекста.</summary>
    [Theory]
    [InlineData(SizeStyleEnum.ExtraSmall, "label-xs")]
    [InlineData(SizeStyleEnum.Small, "label-sm")]
    [InlineData(SizeStyleEnum.Large, "label-lg")]
    public void Size_IsRenderedAsClass(SizeStyleEnum size, string sizeClass)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<JoinBadge>(p => p
            .Add(x => x.Text, "бейдж")
            .Add(x => x.Size, size));

        cut.Find("span").ClassList.ShouldContain(sizeClass);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(SizeStyleEnum.Medium)]
    public void WithoutSizeOrMedium_NoSizeClass(SizeStyleEnum? size)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<JoinBadge>(p =>
        {
            p.Add(x => x.Text, "бейдж");
            if (size is { } value)
            {
                p.Add(x => x.Size, value);
            }
        });

        cut.Find("span").ClassList.ShouldBe(["label", "label-default"]);
    }
}
