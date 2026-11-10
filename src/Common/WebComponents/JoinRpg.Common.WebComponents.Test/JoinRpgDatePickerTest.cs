using Microsoft.AspNetCore.Components.Forms;

namespace JoinRpg.Common.WebComponents.Test;

public class JoinRpgDatePickerTest
{
    private sealed class FormModel
    {
        public DateOnly? OperationDate { get; set; }
    }

    [Fact]
    public void RendersDateInputWithNameValueMinMax()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinRpgDatePicker>(p => p
            .Add(x => x.Name, "OperationDate")
            .Add(x => x.Value, new DateOnly(2026, 1, 15))
            .Add(x => x.Min, new DateOnly(2026, 1, 1))
            .Add(x => x.Max, new DateOnly(2026, 12, 31)));

        var input = cut.Find("input");
        input.GetAttribute("type").ShouldBe("date");
        input.GetAttribute("name").ShouldBe("OperationDate");
        input.GetAttribute("value").ShouldBe("2026-01-15");
        input.GetAttribute("min").ShouldBe("2026-01-01");
        input.GetAttribute("max").ShouldBe("2026-12-31");
    }

    [Fact]
    public void Change_ValidDate_InvokesValueChangedWithParsedDate()
    {
        using var ctx = new BunitContext();
        DateOnly? captured = null;
        var cut = ctx.Render<JoinRpgDatePicker>(p => p
            .Add(x => x.Name, "OperationDate")
            .Add(x => x.ValueChanged, v => captured = v));

        cut.Find("input").Change("2026-01-15");

        captured.ShouldBe(new DateOnly(2026, 1, 15));
    }

    [Fact]
    public void Change_EmptyValue_InvokesValueChangedWithNull()
    {
        using var ctx = new BunitContext();
        DateOnly? captured = new DateOnly(2026, 1, 15);
        var cut = ctx.Render<JoinRpgDatePicker>(p => p
            .Add(x => x.Name, "OperationDate")
            .Add(x => x.Value, new DateOnly(2026, 1, 15))
            .Add(x => x.ValueChanged, v => captured = v));

        cut.Find("input").Change("");

        captured.ShouldBeNull();
    }

    /// <summary>
    /// Оформление поля — в scoped CSS компонента, опора на Bootstrap (class="form-control")
    /// запрещена: без Bootstrap CSS поле должно выглядеть так же.
    /// </summary>
    [Fact]
    public void Input_DoesNotUseBootstrapFormControlClass()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinRpgDatePicker>(p => p.Add(x => x.Name, "OperationDate"));

        var input = cut.Find("input");
        input.HasAttribute("class").ShouldBeFalse();
        input.HasAttribute("style").ShouldBeFalse();
    }

    /// <summary>
    /// В <c>EditForm</c> валидация перерисовывает сообщение об ошибке и состояние кнопки по событию
    /// <c>OnFieldChanged</c>. Без уведомления ввод даты не гасит ошибку — выглядит как «валидация
    /// не отпускает, хотя дату ввёл».
    /// </summary>
    [Fact]
    public void Change_NotifiesEditContextThatFieldChanged()
    {
        using var ctx = new BunitContext();
        var model = new FormModel();
        var editContext = new EditContext(model);
        var changedFields = new List<string>();
        editContext.OnFieldChanged += (_, args) => changedFields.Add(args.FieldIdentifier.FieldName);

        var cut = ctx.Render<JoinRpgDatePicker>(p => p
            .AddCascadingValue(editContext)
            .Add(x => x.Name, nameof(FormModel.OperationDate))
            .Add(x => x.Value, model.OperationDate)
            .Add(x => x.ValueChanged, v => model.OperationDate = v)
            .Add(x => x.ValueExpression, () => model.OperationDate));

        cut.Find("input").Change("2026-05-01");

        model.OperationDate.ShouldBe(new DateOnly(2026, 5, 1));
        changedFields.ShouldBe([nameof(FormModel.OperationDate)]);
    }
}
