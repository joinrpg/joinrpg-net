namespace JoinRpg.Web.ProjectMasterTools.Fields;

public class FieldVariantListItemViewModel : IMoveableListItem
{
    public required ProjectFieldVariantIdentification VariantId { get; init; }
    public required string Label { get; init; }
    public string? Description { get; init; }
    public int Price { get; init; }
    public bool IsActive { get; init; }
    public bool MasterRestricted { get; init; }

    string IMoveableListItem.Id => VariantId.ToString()!;
    string IMoveableListItem.ParentId => VariantId.FieldId.ToString()!;
    string IMoveableListItem.DisplayText => Label;
    string IMoveableListItem.Subtext => Description ?? "";
}
