using JoinRpg.WebComponents;

namespace JoinRpg.Web.Plots;

internal sealed class PlotElementMoveItem(PlotRenderedTextViewModel plot, CharacterIdentification characterId) : IMoveableListItem
{
    public PlotRenderedTextViewModel Plot { get; } = plot;

    string IMoveableListItem.Id => Plot.PlotVersionId.PlotElementId.ToString();
    string IMoveableListItem.ParentId => characterId.ToString();
    string IMoveableListItem.DisplayText => Plot.ShortContent;
    string IMoveableListItem.Subtext => Plot.Todo ?? "";
}
