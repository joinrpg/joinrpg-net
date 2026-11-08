using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace JoinRpg.Common.WebComponents.Test;

public class JoinInputNumberTest
{
    [Theory]
    [InlineData("42", 42)]
    [InlineData("-5", -5)]
    [InlineData("0", 0)]
    [InlineData("", null)]
    [InlineData("1.5", null)]
    [InlineData("1e3", null)]
    [InlineData("99999999999", null)]
    public void Change_ParsesOrNulls_ForNullableIntWithoutEditContext(string input, int? expected)
    {
        using var ctx = new BunitContext();
        int? captured = 7;
        var cut = ctx.Render<JoinInputNumber<int?>>(p => p
            .Add(x => x.Value, null)
            .Add(x => x.ValueChanged, v => captured = v));

        cut.Find("input").Change(input);

        captured.ShouldBe(expected);
    }

    [Fact]
    public void Change_Garbage_DoesNotInvokeValueChanged_ForNonNullableIntWithoutEditContext()
    {
        using var ctx = new BunitContext();
        var captured = 7;
        var cut = ctx.Render<JoinInputNumber<int>>(p => p
            .Add(x => x.Value, 0)
            .Add(x => x.ValueChanged, v => captured = v));

        cut.Find("input").Change("abc");

        captured.ShouldBe(7);
    }

    [Fact]
    public void Change_ParsesFraction_ForDecimal()
    {
        using var ctx = new BunitContext();
        decimal? captured = null;
        var cut = ctx.Render<JoinInputNumber<decimal?>>(p => p
            .Add(x => x.Value, null)
            .Add(x => x.ValueChanged, v => captured = v));

        cut.Find("input").Change("1.5");

        captured.ShouldBe(1.5m);
    }

    [Fact]
    public void MinMaxName_RenderAsAttributes_OnlyWhenSet()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinInputNumber<int?>>(p => p
            .Add(x => x.Value, null)
            .Add(x => x.Name, "Qty")
            .Add(x => x.Min, 0)
            .Add(x => x.Max, 100));

        var input = cut.Find("input");
        input.GetAttribute("name").ShouldBe("Qty");
        input.GetAttribute("min").ShouldBe("0");
        input.GetAttribute("max").ShouldBe("100");

        var plain = ctx.Render<JoinInputNumber<int?>>(p => p.Add(x => x.Value, null));
        var plainInput = plain.Find("input");
        plainInput.HasAttribute("name").ShouldBeFalse();
        plainInput.HasAttribute("min").ShouldBeFalse();
        plainInput.HasAttribute("max").ShouldBeFalse();
    }

    [Fact]
    public void AdditionalAttributes_MinReachesInput()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinInputNumber<int?>>(p => p
            .Add(x => x.Value, null)
            .Add(x => x.AdditionalAttributes, new Dictionary<string, object> { ["min"] = 10 }));

        cut.Find("input").GetAttribute("min").ShouldBe("10");
    }

    [Fact]
    public void ValueZero_RendersValueAttribute_EmptyForNull()
    {
        using var ctx = new BunitContext();
        var zero = ctx.Render<JoinInputNumber<int>>(p => p.Add(x => x.Value, 0));
        zero.Find("input").GetAttribute("value").ShouldBe("0");

        var empty = ctx.Render<JoinInputNumber<int?>>(p => p.Add(x => x.Value, null));
        empty.Find("input").GetAttribute("value").ShouldBe("");
    }

    [Fact]
    public void ExternalValueChange_UpdatesValueAttribute()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinInputNumber<int?>>(p => p.Add(x => x.Value, null));
        cut.Find("input").GetAttribute("value").ShouldBe("");

        cut.Render(p => p.Add(x => x.Value, 42));

        cut.Find("input").GetAttribute("value").ShouldBe("42");
    }

    [Fact]
    public void Change_Garbage_KeepsRawText_AfterExternalRerenderWithoutValueChange()
    {
        using var ctx = new BunitContext();
        var captured = 7;
        var cut = ctx.Render<JoinInputNumber<int>>(p => p
            .Add(x => x.Value, 0)
            .Add(x => x.ValueChanged, v => captured = v));

        cut.Find("input").Change("abc");
        captured.ShouldBe(7);

        // Родительский ре-рендер без изменения Value (поменялся другой параметр)
        // не должен стирать сырое значение после ошибки парсинга
        cut.Render(p => p
            .Add(x => x.Value, 0)
            .Add(x => x.Name, "Qty")
            .Add(x => x.ValueChanged, v => captured = v));

        cut.Find("input").GetAttribute("value").ShouldBe("abc");
    }

    [Fact]
    public void Change_Garbage_AddsValidationMessageAndKeepsRawText_ForNonNullableIntInEditForm()
    {
        using var ctx = new BunitContext();
        var model = new FormModel();
        var captured = 7;
        var cut = ctx.Render(FormFragment(model, v => captured = v));

        cut.Find("input").Change("abc");

        var editContext = cut.FindComponent<EditForm>().Instance.EditContext.ShouldNotBeNull();
        editContext.GetValidationMessages().ShouldContain(m => m.Contains("Укажите число"));
        captured.ShouldBe(7);
        model.Number.ShouldBe(0);
        cut.Find("input").GetAttribute("value").ShouldBe("abc");
    }

    [Fact]
    public void Change_ValidValue_InvokesAndNotifiesEditForm()
    {
        using var ctx = new BunitContext();
        var model = new FormModel();
        var captured = 0;
        var cut = ctx.Render(FormFragment(model, v => captured = v));

        cut.Find("input").Change("42");

        captured.ShouldBe(42);
        model.Number.ShouldBe(42);
        var editContext = cut.FindComponent<EditForm>().Instance.EditContext.ShouldNotBeNull();
        editContext.GetValidationMessages().ShouldBeEmpty();
    }

    private static RenderFragment FormFragment(FormModel model, Action<int> onValueChanged) => builder =>
    {
        builder.OpenComponent<EditForm>(0);
        builder.AddAttribute(1, nameof(EditForm.Model), model);
        builder.AddAttribute(2, nameof(EditForm.ChildContent), (RenderFragment<EditContext>)(_ => childBuilder =>
        {
            childBuilder.OpenComponent<JoinInputNumber<int>>(0);
            childBuilder.AddAttribute(1, nameof(JoinInputNumber<int>.Value), model.Number);
            childBuilder.AddAttribute(2, nameof(JoinInputNumber<int>.ValueChanged),
                EventCallback.Factory.Create<int>(model, v =>
                {
                    model.Number = v;
                    onValueChanged(v);
                }));
            childBuilder.AddAttribute(3, nameof(JoinInputNumber<int>.ValueExpression),
                (Expression<Func<int>>)(() => model.Number));
            childBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    };

    private sealed class FormModel
    {
        public int Number { get; set; }
    }
}
