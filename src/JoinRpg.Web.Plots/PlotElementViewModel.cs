using static JoinRpg.Web.Plots.PlotStatus;

namespace JoinRpg.Web.Plots;

// Content хранится как string, а не MarkupString: MarkupString не переживает
// JSON-сериализацию параметров компонента на границе WebAssemblyPrerendered
// (нет публичного конструктора для десериализации) — текст просто пропадает при гидратации.
public record class PlotRenderedTextViewModel(string Content, string ShortContent, string? Todo, PlotVersionIdentification PlotVersionId, PlotStatus? PlotStatus, TargetsInfo? Target)
{
    public bool HasWorkTodo => !string.IsNullOrWhiteSpace(Todo) || PlotStatus == InWork || PlotStatus == HasNewVersion;
}
