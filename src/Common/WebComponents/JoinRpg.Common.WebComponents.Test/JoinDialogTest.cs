using JoinRpg.Common.WebComponents.Dialog;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Common.WebComponents.Test;

public class JoinDialogTest
{
    /// <summary>
    /// Родитель получает <c>OnAfterRenderAsync</c> раньше дочернего диалога, то есть до того, как
    /// диалог успел бы загрузить свой JS-модуль. Открыть диалог оттуда всё равно должно получиться —
    /// так страница комнат сама предлагает добавить комнаты, если их ещё нет.
    /// </summary>
    [Fact]
    public void CanBeOpenedFromParentOnAfterRender()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        ctx.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        var module = ctx.JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        _ = module.SetupVoid("showModal", _ => true);
        _ = module.SetupVoid("closeModal", _ => true);

        var cut = ctx.Render<OpensDialogOnFirstRender>();

        cut.WaitForAssertion(() => module.Invocations["showModal"].ShouldHaveSingleItem());
    }

    private sealed class OpensDialogOnFirstRender : ComponentBase
    {
        private JoinDialog dialog = null!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<JoinDialog>(0);
            builder.AddAttribute(1, nameof(JoinDialog.ChildContent), (RenderFragment)(b => b.AddContent(0, "Диалог")));
            builder.AddComponentReferenceCapture(2, reference => dialog = (JoinDialog)reference);
            builder.CloseComponent();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                dialog.ShowModal();
            }
        }
    }
}
