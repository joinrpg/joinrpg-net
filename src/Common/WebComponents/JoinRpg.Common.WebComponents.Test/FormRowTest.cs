using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Common.WebComponents.Test;

public class FormRowTest
{
    [Fact]
    public void Description_RendersDescriptionText()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<FormRow>(p => p
            .Add(x => x.Label, "Label")
            .Add(x => x.Description, "Some description"));
        cut.Markup.ShouldContain("Some description");
    }

    [Fact]
    public void DescriptionFragment_RendersFragmentContent()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<FormRow>(p => p
            .Add(x => x.Label, "Label")
            .Add(x => x.DescriptionFragment, builder => builder.AddContent(0, "Fragment description")));
        cut.Markup.ShouldContain("Fragment description");
    }

    [Fact]
    public void WithoutField_LabelHasNoFor()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<FormRow>(p => p
            .Add(x => x.Label, "Label")
            .AddChildContent("<button type=\"submit\">Сохранить</button>"));
        cut.Find("label").HasAttribute("for").ShouldBeFalse();
    }

    /// <summary>
    /// Каждое поле из Common, положенное в <see cref="FormRowFor{T}"/> без явного id, само связывается с подписью:
    /// клик по подписи ставит фокус в поле.
    /// </summary>
    [Theory]
    [InlineData(nameof(JoinTextInput), "INPUT")]
    [InlineData(nameof(JoinTextArea), "TEXTAREA")]
    [InlineData(nameof(CheckboxInput), "INPUT")]
    [InlineData(nameof(NumberInput), "INPUT")]
    [InlineData(nameof(JoinInputNumber<>), "INPUT")]
    [InlineData(nameof(JoinRpgDatePicker), "INPUT")]
    [InlineData(nameof(JoinRpgTimePicker), "INPUT")]
    [InlineData(nameof(EnumSelector<>), "SELECT")]
    [InlineData(nameof(StringInput), "INPUT")]
    public void FormRowFor_LinksLabelToField(string field, string tagName)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var model = new Model();
        var cut = ctx.Render<FormRowFor<string>>(p => p
            .Add(x => x.For, () => model.Text)
            .AddChildContent(Field(field, model)));

        var target = cut.Find("label").GetAttribute("for");
        target.ShouldNotBeNullOrEmpty();
        cut.FindAll($"[id='{target}']").ShouldHaveSingleItem().TagName.ShouldBe(tagName);
    }

    [Fact]
    public void FormRowFor_GeneratedIdContainsFieldName()
    {
        using var ctx = new BunitContext();
        var model = new Model();
        var cut = ctx.Render<FormRowFor<string>>(p => p
            .Add(x => x.For, () => model.Text)
            .AddChildContent(Field(nameof(JoinTextInput), model)));

        cut.Find("input").Id.ShouldNotBeNull().ShouldContain(nameof(Model.Text));
    }

    [Fact]
    public void ExplicitFieldId_IsUsedByLabel()
    {
        using var ctx = new BunitContext();
        var model = new Model();
        var cut = ctx.Render<FormRow>(p => p
            .Add(x => x.Label, "Label")
            .AddChildContent<JoinTextInput>(f => f
                .Add(x => x.Value, model.Text)
                .AddUnmatched("id", "my-field")));

        cut.Find("label").GetAttribute("for").ShouldBe("my-field");
        cut.Find("input").Id.ShouldBe("my-field");
    }

    /// <summary>
    /// Подпись одна — ссылается на первое поле строки. Второе поле не получает того же id,
    /// иначе в документе оказались бы два элемента с одним id.
    /// </summary>
    [Fact]
    public void TwoFieldsInRow_LabelPointsToFirst_IdsAreUnique()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<FormRow>(p => p
            .Add(x => x.Label, "Когда")
            .AddChildContent<JoinRpgDatePicker>(f => f.Add(x => x.Name, "date"))
            .AddChildContent<JoinRpgTimePicker>(f => f.Add(x => x.Name, "time")));

        var target = cut.Find("label").GetAttribute("for");
        cut.FindAll($"[id='{target}']").ShouldHaveSingleItem().GetAttribute("type").ShouldBe("date");
        cut.Find("input[type=time]").HasAttribute("id").ShouldBeFalse();
    }

    [Fact]
    public void EachRow_GetsOwnId()
    {
        using var ctx = new BunitContext();
        var model = new Model();
        RenderFragment rows = builder =>
        {
            for (var i = 0; i < 2; i++)
            {
                builder.OpenComponent<FormRowFor<string>>(0);
                builder.AddComponentParameter(1, nameof(FormRowFor<>.For), (System.Linq.Expressions.Expression<Func<string>>)(() => model.Text));
                builder.AddComponentParameter(2, nameof(FormRowFor<>.ChildContent), Field(nameof(JoinTextInput), model));
                builder.CloseComponent();
            }
        };
        var cut = ctx.Render(rows);

        var ids = cut.FindAll("input").Select(input => input.Id).ToList();
        ids.Distinct().Count().ShouldBe(2);
        cut.FindAll("label").Select(label => label.GetAttribute("for")).ShouldBe(ids);
    }

    private static RenderFragment Field(string field, Model model) => builder =>
    {
        switch (field)
        {
            case nameof(JoinTextInput):
                builder.OpenComponent<JoinTextInput>(0);
                builder.AddComponentParameter(1, nameof(JoinTextInput.Value), model.Text);
                break;
            case nameof(JoinTextArea):
                builder.OpenComponent<JoinTextArea>(0);
                builder.AddComponentParameter(1, nameof(JoinTextArea.Value), model.Text);
                break;
            case nameof(CheckboxInput):
                builder.OpenComponent<CheckboxInput>(0);
                builder.AddComponentParameter(1, nameof(CheckboxInput.ValueExpression), (System.Linq.Expressions.Expression<Func<bool>>)(() => model.Flag));
                break;
            case nameof(NumberInput):
                builder.OpenComponent<NumberInput>(0);
                break;
            case nameof(JoinInputNumber<>):
                builder.OpenComponent<JoinInputNumber<int>>(0);
                builder.AddComponentParameter(1, nameof(JoinInputNumber<>.ValueExpression), (System.Linq.Expressions.Expression<Func<int>>)(() => model.Number));
                break;
            case nameof(JoinRpgDatePicker):
                builder.OpenComponent<JoinRpgDatePicker>(0);
                builder.AddComponentParameter(1, nameof(JoinRpgDatePicker.Name), "date");
                break;
            case nameof(JoinRpgTimePicker):
                builder.OpenComponent<JoinRpgTimePicker>(0);
                builder.AddComponentParameter(1, nameof(JoinRpgTimePicker.Name), "time");
                break;
            case nameof(EnumSelector<>):
                builder.OpenComponent<EnumSelector<DayOfWeek>>(0);
                builder.AddComponentParameter(1, nameof(EnumSelector<>.Name), "day");
                break;
            case nameof(StringInput):
                builder.OpenComponent<StringInput>(0);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field), field, null);
        }
        builder.CloseComponent();
    };

    private class Model
    {
        [Display(Name = "Текст")]
        public string Text { get; set; } = "";
        public bool Flag { get; set; }
        public int Number { get; set; }
    }
}
