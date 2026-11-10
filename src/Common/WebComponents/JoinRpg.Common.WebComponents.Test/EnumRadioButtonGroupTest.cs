using Microsoft.AspNetCore.Components.Forms;

namespace JoinRpg.Common.WebComponents.Test;

public class EnumRadioButtonGroupTest
{
    private enum Choice
    {
        First,
        Second,
    }

    private sealed class FormModel
    {
        public Choice Mode { get; set; }
    }

    /// <summary>
    /// Компонент умеет работать без <c>SelectedValueExpression</c> (все вызовы в порталах идут через
    /// <c>@bind</c>, но выражение — необязательный параметр): раньше он падал в <c>OnInitialized</c>,
    /// если выражение не передано.
    /// </summary>
    [Fact]
    public void WithoutExpression_DoesNotThrowAndDoesNotNotify()
    {
        using var ctx = new BunitContext();
        var editContext = new EditContext(new FormModel());
        var notifications = 0;
        editContext.OnFieldChanged += (_, _) => notifications++;

        var cut = ctx.Render<EnumRadioButtonGroup<Choice>>(p => p
            .AddCascadingValue(editContext)
            .Add(x => x.SelectedValue, Choice.First));

        cut.FindAll("input")[1].Change("Second");

        notifications.ShouldBe(0);
    }

    /// <summary>
    /// В <c>EditForm</c> валидация перерисовывает сообщение об ошибке и состояние кнопки по событию
    /// <c>OnFieldChanged</c> — выбор пункта должен его порождать.
    /// </summary>
    [Fact]
    public void Selecting_NotifiesEditContextThatFieldChanged()
    {
        using var ctx = new BunitContext();
        var model = new FormModel();
        var editContext = new EditContext(model);
        var changedFields = new List<string>();
        editContext.OnFieldChanged += (_, args) => changedFields.Add(args.FieldIdentifier.FieldName);

        var cut = ctx.Render<EnumRadioButtonGroup<Choice>>(p => p
            .AddCascadingValue(editContext)
            .Add(x => x.SelectedValue, model.Mode)
            .Add(x => x.SelectedValueExpression, () => model.Mode)
            .Add(x => x.SelectedValueChanged, v => model.Mode = v));

        cut.FindAll("input")[1].Change("Second");

        model.Mode.ShouldBe(Choice.Second);
        changedFields.ShouldBe([nameof(FormModel.Mode)]);
    }
}
