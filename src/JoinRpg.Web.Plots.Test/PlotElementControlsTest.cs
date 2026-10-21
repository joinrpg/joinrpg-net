using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Web.Plots.Elements;
using JoinRpg.Web.Plots.Folders;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Web.Plots.Test;

/// <summary>
/// Кнопки-остров вводной (удалить, скрыть, опубликовать): причину отказа сервера (например, проект заблокирован)
/// показывают, а не падают молча (#5735).
/// </summary>
public class PlotElementControlsTest : BunitContext
{
    private static readonly PlotElementIdentification ElementId = new(new PlotFolderIdentification(1, 2), 3);

    private readonly FakeClient client = new();

    public PlotElementControlsTest()
    {
        Services.AddSingleton<IPlotClient>(client);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    private async Task<IRenderedComponent<PlotElementControls>> ClickDeleteAndConfirm()
    {
        var cut = Render<PlotElementControls>(p => p.Add(c => c.Model, new PlotElementControlsViewModel(
            PublishedVersion: null,
            HasEditAccess: true,
            PlotStatus.InWork,
            ElementId,
            CurrentVersion: new PlotVersionIdentification(ElementId, 1),
            CouldBePublished: false)));
        var click = cut.FindAll("button")
            .Single(b => b.TextContent.Trim() == "Удалить" && b.Closest("dialog") is null)
            .ClickAsync(new MouseEventArgs());
        var confirm = cut.FindAll("dialog").Single(d => d.TextContent.Contains("Удалить вводную"));
        await confirm.QuerySelectorAll("button").Single(b => b.TextContent.Trim() == "Удалить").ClickAsync(new MouseEventArgs());
        await cut.FindAll("dialog").Single(d => d.TextContent.Contains("Удалить вводную"))
            .TriggerEventAsync("onclose", EventArgs.Empty);
        await click;
        return cut;
    }

    [Fact]
    public async Task RefusalReasonIsShown()
    {
        client.Failure = new ServerProblemException("Проект заблокирован на время восстановления данных, изменения в нём сейчас невозможны");

        var cut = await ClickDeleteAndConfirm();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain(
            "Проект заблокирован на время восстановления данных, изменения в нём сейчас невозможны"));
        client.Deleted.ShouldBe([ElementId]);
    }

    [Fact]
    public async Task UnexpectedFailureShowsGenericText()
    {
        client.Failure = new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).");

        var cut = await ClickDeleteAndConfirm();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось удалить вводную"));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }

    private sealed class FakeClient : IPlotClient
    {
        public Exception? Failure { get; set; }
        public List<PlotElementIdentification> Deleted { get; } = [];

        public Task DeleteElement(PlotElementIdentification elementId)
        {
            Deleted.Add(elementId);
            return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        }

        public Task<PlotFolderDto[]> GetPlotFoldersList(ProjectIdentification projectId) => throw new NotSupportedException();
        public Task UnPublishVersion(PlotVersionIdentification version) => throw new NotSupportedException();
        public Task PublishVersion(PublishPlotElementViewModel model) => throw new NotSupportedException();
        public Task UnDeleteElement(PlotElementIdentification elementId) => throw new NotSupportedException();
    }
}
