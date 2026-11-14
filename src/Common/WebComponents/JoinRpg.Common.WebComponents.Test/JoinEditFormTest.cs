using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Обёртка формы: в статическом рендере содержимое не рендерится вовсе (иначе форма с
/// префиксными именами полей постилась бы нативно и ломала бы биндинг), в интерактивном —
/// поля получают имена с префиксом FormName, обработчики EditForm пробрасываются.
/// </summary>
public class JoinEditFormTest
{
    private class FormModel
    {
        public string Name { get; set; } = "";
    }

    private static BunitContext NewContext(bool interactive)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: interactive));
        return ctx;
    }

    [Fact]
    public void StaticRender_ShowsLoadingInsteadOfForm()
    {
        using var ctx = NewContext(interactive: false);
        var cut = ctx.Render<JoinEditForm>(p => p
            .Add(x => x.FormName, "Test")
            .Add(x => x.EditContext, new EditContext(new FormModel()))
            .AddChildContent("<p>секретная форма</p>"));

        cut.Markup.ShouldNotContain("<form");
        cut.Markup.ShouldNotContain("секретная форма");
        cut.Markup.ShouldContain("Идет загрузка");
    }

    [Fact]
    public void InteractiveRender_ChildInputNamePrefixedWithFormName()
    {
        using var ctx = NewContext(interactive: true);
        var model = new FormModel();
        var cut = ctx.Render<JoinEditForm>(p => p
            .Add(x => x.FormName, "AddPaymentType")
            .Add(x => x.EditContext, new EditContext(model))
            .AddChildContent<JoinTextInput>(f => f
                .Add(x => x.Value, model.Name)
                .Add(x => x.ValueChanged, _ => { })
                .Add(x => x.ValueExpression, () => model.Name)));

        cut.Find("input").GetAttribute("name").ShouldBe("AddPaymentType.Name");
    }

    [Fact]
    public void OnValidSubmit_PassedThrough()
    {
        using var ctx = NewContext(interactive: true);
        var submitted = false;
        var cut = ctx.Render<JoinEditForm>(p => p
            .Add(x => x.FormName, "Test")
            .Add(x => x.EditContext, new EditContext(new FormModel()))
            .Add(x => x.OnValidSubmit, () => { submitted = true; })
            .AddChildContent("<button type=\"submit\">Сохранить</button>"));

        cut.Find("form").Submit();

        submitted.ShouldBeTrue();
    }

    [Fact]
    public void OnSubmit_PassedThrough()
    {
        using var ctx = NewContext(interactive: true);
        var submitted = false;
        var cut = ctx.Render<JoinEditForm>(p => p
            .Add(x => x.FormName, "Test")
            .Add(x => x.EditContext, new EditContext(new FormModel()))
            .Add(x => x.OnSubmit, () => { submitted = true; })
            .AddChildContent("<button type=\"submit\">Сохранить</button>"));

        cut.Find("form").Submit();

        submitted.ShouldBeTrue();
    }
}
