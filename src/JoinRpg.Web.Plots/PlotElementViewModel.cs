using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.Plots;

public class PlotElementViewModel
{
    public PlotStatus? Status { get; }
    public MarkupString Content { get; }
    public MarkupString? TodoField { get; }
    public PlotElementIdentification PlotElementId { get; }

    public bool ShowEditControls { get; }

    public bool First { get; set; }
    public bool Last { get; set; }

    public TargetsInfo? Target { get; }

    public PlotRenderedTextViewModel PlotText { get; }

    public PlotElementViewModel(CharacterIdentification? characterId,
        PlotRenderedTextViewModel plotText,
        bool showEditControls)
    {
        ArgumentNullException.ThrowIfNull(plotText);

        Content = plotText.Content;
        TodoField = plotText.Todo;
        ShowEditControls = showEditControls;
        PlotElementId = plotText.PlotVersionId.PlotElementId;
        Status = plotText.PlotStatus;
        Target = plotText.Target;
        CharacterId = characterId;
    }

    public CharacterIdentification? CharacterId { get; }

    public bool HasWorkTodo => !string.IsNullOrWhiteSpace(TodoField?.Value) || Status == PlotStatus.InWork || Status == PlotStatus.HasNewVersion;
}

public record class PlotRenderedTextViewModel(MarkupString Content, MarkupString? Todo, PlotVersionIdentification PlotVersionId, PlotStatus? PlotStatus, TargetsInfo? Target);
