using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace JoinRpg.Common.WebComponents.Test;

public class JoinFormDialogTest
{
    private class FormModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Value { get; set; } = "";
    }

    private static IRenderedComponent<JoinFormDialog<FormModel>> RenderDialog(BunitContext ctx, FormModel model)
        => ctx.Render<JoinFormDialog<FormModel>>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.FormName, "Test")
            .Add(x => x.SubmitButtonPreset, ButtonPreset.Save)
            .Add(x => x.ChildContent, builder => builder.AddMarkupContent(0, "<span>поле</span>")));

    private static bool SubmitDisabled(IRenderedComponent<JoinFormDialog<FormModel>> cut)
        => cut.FindAll(".join-dialog-footer button")[0].HasAttribute("disabled");

    /// <summary>
    /// Кнопка отправки следует атрибутам валидации модели и пересчитывается при каждой перерисовке
    /// родителя: поля вроде JoinRpgDatePicker не сообщают EditContext об изменениях.
    /// </summary>
    [Fact]
    public void SubmitButton_FollowsModelValidity()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var model = new FormModel();

        var cut = RenderDialog(ctx, model);
        SubmitDisabled(cut).ShouldBeTrue();

        model.Value = "заполнено";
        cut.Render(p => p
            .Add(x => x.Model, model)
            .Add(x => x.FormName, "Test")
            .Add(x => x.SubmitButtonPreset, ButtonPreset.Save)
            .Add(x => x.ChildContent, builder => builder.AddMarkupContent(0, "<span>поле</span>")));

        SubmitDisabled(cut).ShouldBeFalse();
    }

    private class EditContextProbe : ComponentBase
    {
        [CascadingParameter]
        public EditContext? EditContext { get; set; }

        public static List<EditContext?> Seen { get; } = [];

        protected override void OnInitialized() => Seen.Add(EditContext);
    }

    /// <summary>
    /// Родитель перерисовывается на каждый ввод в поле с Immediate. Если диалог при этом пересоздаёт
    /// EditContext, EditForm пересоздаёт всё содержимое, и поле теряет фокус после каждого символа.
    /// </summary>
    [Fact]
    public void ParentRerender_KeepsEditContext()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        EditContextProbe.Seen.Clear();
        var model = new FormModel();
        RenderFragment content = builder =>
        {
            builder.OpenComponent<EditContextProbe>(0);
            builder.CloseComponent();
        };

        var cut = ctx.Render<JoinFormDialog<FormModel>>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.FormName, "Test")
            .Add(x => x.SubmitButtonPreset, ButtonPreset.Save)
            .Add(x => x.ChildContent, content));

        cut.Render(p => p
            .Add(x => x.Model, model)
            .Add(x => x.FormName, "Test")
            .Add(x => x.SubmitButtonPreset, ButtonPreset.Save)
            .Add(x => x.ChildContent, content));

        // Содержимое формы создано один раз и не пересоздавалось при перерисовке
        EditContextProbe.Seen.ShouldHaveSingleItem().ShouldNotBeNull();
    }

    /// <summary>
    /// Закрытие диалога перерисовывает JoinDialog вместе с содержимым формы. Раньше форма к этому
    /// моменту уже обнуляла свой EditContext, и EditForm падал при рендере.
    /// </summary>
    [Fact]
    public async Task Submit_ClosesWithoutRenderFailure_AndReturnsModel()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var model = new FormModel { Value = "заполнено" };
        var cut = RenderDialog(ctx, model);

        var shown = cut.InvokeAsync(() => cut.Instance.ShowModalAsync());
        await cut.FindAll(".join-dialog-footer button")[0].ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await cut.Find("dialog").TriggerEventAsync("onclose", EventArgs.Empty);

        (await shown).ShouldBeSameAs(model);
    }
}
