using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.Games.FieldSetup;

public class GameFieldListItemViewModel : IMoveableNonInteractiveListItem
{
    public required ProjectFieldIdentification Id { get; init; }
    public required string Name { get; init; }
    public bool IsActive { get; init; }
    public bool IsPublic { get; init; }
    public bool CanPlayerView { get; init; }
    public bool CanPlayerEdit { get; init; }
    public required IReadOnlyList<string> GroupNames { get; init; }
    public MandatoryStatusViewType MandatoryStatus { get; init; }
    public ProjectFieldViewType FieldViewType { get; init; }
    public FieldBoundToViewModel FieldBoundTo { get; init; }
    public int Price { get; init; }

    public string? DescriptionHtml { get; init; }
    public MarkupString? Description => DescriptionHtml is null ? null : new MarkupString(DescriptionHtml);

    public string? MasterDescriptionHtml { get; init; }
    public MarkupString? MasterDescription => MasterDescriptionHtml is null ? null : new MarkupString(MasterDescriptionHtml);

    public bool HasValueList { get; init; }
    public required IReadOnlyList<GameFieldVariantDisplayItem> Variants { get; init; }
    public bool WasEverUsed { get; init; }
    public bool CanEditFields { get; init; }

    public bool First { get; set; }
    public bool Last { get; set; }

    int IMoveableNonInteractiveListItem.ProjectId => Id.ProjectId.Value;
    int IMoveableNonInteractiveListItem.ItemId => Id.ProjectFieldId;
}
