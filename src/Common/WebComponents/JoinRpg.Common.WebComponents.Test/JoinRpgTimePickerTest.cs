using Microsoft.AspNetCore.Components.Forms;

namespace JoinRpg.Common.WebComponents.Test;

public class JoinRpgTimePickerTest
{
    private sealed class FormModel
    {
        public TimeOnly? StartTime { get; set; }
    }

    [Fact]
    public void RendersTimeInputWithNameValue()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinRpgTimePicker>(p => p
            .Add(x => x.Name, "StartTime")
            .Add(x => x.Value, new TimeOnly(9, 5)));

        var input = cut.Find("input");
        input.GetAttribute("type").ShouldBe("time");
        input.GetAttribute("name").ShouldBe("StartTime");
        input.GetAttribute("value").ShouldBe("09:05");
    }

    [Theory]
    [InlineData("10:30")]
    [InlineData("10:30:00")]
    public void Change_ValidTime_InvokesValueChangedWithParsedTime(string value)
    {
        using var ctx = new BunitContext();
        TimeOnly? captured = null;
        var cut = ctx.Render<JoinRpgTimePicker>(p => p
            .Add(x => x.Name, "StartTime")
            .Add(x => x.ValueChanged, v => captured = v));

        cut.Find("input").Change(value);

        captured.ShouldBe(new TimeOnly(10, 30));
    }

    [Fact]
    public void Change_EmptyValue_InvokesValueChangedWithNull()
    {
        using var ctx = new BunitContext();
        TimeOnly? captured = new TimeOnly(10, 30);
        var cut = ctx.Render<JoinRpgTimePicker>(p => p
            .Add(x => x.Name, "StartTime")
            .Add(x => x.Value, new TimeOnly(10, 30))
            .Add(x => x.ValueChanged, v => captured = v));

        cut.Find("input").Change("");

        captured.ShouldBeNull();
    }

    /// <summary>
    /// В <c>EditForm</c> валидация перерисовывает сообщение об ошибке и состояние кнопки по событию
    /// <c>OnFieldChanged</c>. Без уведомления ввод времени не гасит ошибку — выглядит как «валидация
    /// не отпускает, хотя время ввёл».
    /// </summary>
    [Fact]
    public void Change_NotifiesEditContextThatFieldChanged()
    {
        using var ctx = new BunitContext();
        var model = new FormModel();
        var editContext = new EditContext(model);
        var changedFields = new List<string>();
        editContext.OnFieldChanged += (_, args) => changedFields.Add(args.FieldIdentifier.FieldName);

        var cut = ctx.Render<JoinRpgTimePicker>(p => p
            .AddCascadingValue(editContext)
            .Add(x => x.Name, nameof(FormModel.StartTime))
            .Add(x => x.Value, model.StartTime)
            .Add(x => x.ValueChanged, v => model.StartTime = v)
            .Add(x => x.ValueExpression, () => model.StartTime));

        cut.Find("input").Change("10:30");

        model.StartTime.ShouldBe(new TimeOnly(10, 30));
        changedFields.ShouldBe([nameof(FormModel.StartTime)]);
    }
}
