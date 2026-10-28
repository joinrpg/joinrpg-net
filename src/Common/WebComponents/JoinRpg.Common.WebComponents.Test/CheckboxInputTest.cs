using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Контракт CheckboxInput в обычной MVC-форме: MVC биндит bool по первому значению поля в POST.
/// Проверяем, что первым уходит то, что видит пользователь, — и до загрузки WASM, и после.
/// </summary>
public class CheckboxInputTest
{
    private const string FieldName = "Flag";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StaticRender_PostsValue(bool value)
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, value);

        FirstPostedValue(cut.FindAll("input")).ShouldBe(value);
    }

    /// <summary>
    /// Пока WASM не загрузился, клик меняет только состояние checkbox в DOM, Blazor об этом не знает.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ClickBeforeInteractivity_PostsClickedValue(bool initial)
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, initial);
        var inputs = cut.FindAll("input");

        var checkbox = inputs.OfType<IHtmlInputElement>().Single(i => i.Type == "checkbox");
        checkbox.IsChecked = !initial;

        FirstPostedValue(inputs).ShouldBe(!initial);
    }

    [Fact]
    public void Interactive_ChangePostsNewValueAndNotifies()
    {
        using var ctx = new BunitContext();
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        var changes = new List<bool>();
        var cut = Render(ctx, true, changes.Add);

        cut.Find("input[type=checkbox]").Change(false);
        FirstPostedValue(cut.FindAll("input")).ShouldBe(false);

        cut.Find("input[type=checkbox]").Change(true);
        FirstPostedValue(cut.FindAll("input")).ShouldBe(true);

        changes.ShouldBe([false, true]);
    }

    /// <summary>
    /// Отключённый checkbox браузер не постит — значение должно дойти до сервера неизменным.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Disabled_PostsCurrentValue(bool value)
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, value, disabled: true);

        FirstPostedValue(cut.FindAll("input")).ShouldBe(value);
    }

    // В разметке disabled часто пишут строкой — для Blazor это тоже «отключено».
    [Theory]
    [InlineData("disabled", true)]
    [InlineData("disabled", false)]
    [InlineData("", true)]
    [InlineData("", false)]
    public void DisabledAsString_PostsCurrentValue(string disabled, bool value)
    {
        using var ctx = new BunitContext();
        var model = new Model { Flag = value };
        var cut = ctx.Render<CheckboxInput>(p => p
            .Add(x => x.Value, model.Flag)
            .Add(x => x.ValueExpression, () => model.Flag)
            .AddUnmatched("disabled", disabled));

        FirstPostedValue(cut.FindAll("input")).ShouldBe(value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DisabledFalse_BehavesAsEnabled(bool initial)
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, initial, disabled: false);
        var inputs = cut.FindAll("input");

        var checkbox = inputs.OfType<IHtmlInputElement>().Single(i => i.Type == "checkbox");
        checkbox.IsChecked = !initial;

        FirstPostedValue(inputs).ShouldBe(!initial);
    }

    private sealed class Model
    {
        public bool Flag { get; set; }
    }

    private static IRenderedComponent<CheckboxInput> Render(BunitContext ctx, bool value, Action<bool>? onChange = null, bool? disabled = null)
    {
        var model = new Model { Flag = value };
        return ctx.Render<CheckboxInput>(p =>
        {
            _ = p
                .Add(x => x.Value, model.Flag)
                .Add(x => x.ValueExpression, () => model.Flag)
                .Add(x => x.ValueChanged, v => onChange?.Invoke(v));
            if (disabled is bool d)
            {
                _ = p.AddUnmatched("disabled", d);
            }
        });
    }

    /// <summary>
    /// Значения поля в порядке документа так, как их отправит браузер: hidden всегда,
    /// checkbox — только отмеченный и не отключённый. MVC берёт первое.
    /// </summary>
    private static bool FirstPostedValue(IEnumerable<IElement> inputs)
    {
        var posted = inputs
            .OfType<IHtmlInputElement>()
            .Where(i => i.Name == FieldName)
            .Where(i => i.Type != "checkbox" || (i.IsChecked && !i.IsDisabled))
            .Select(i => i.Value)
            .ToList();

        posted.ShouldNotBeEmpty();
        return bool.Parse(posted[0]);
    }
}
