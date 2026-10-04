using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace JoinRpg.Common.WebComponents.Test;

public class JoinFormDialogTest
{
    private class FormModel
    {
        public string Value { get; set; } = "";
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
}
