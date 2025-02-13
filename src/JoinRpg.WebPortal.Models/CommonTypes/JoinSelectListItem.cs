namespace JoinRpg.Web.Models.CommonTypes;

public class JoinSelectListItem
{
    public string? ExtraSearch { get; set; }
    public string? Subtext { get; set; }

    public bool Disabled { get; set; }
    public bool Selected { get; set; }
    public required string Text { get; set; }
    public required int Value { get; set; }
}

public static class JoinSelectListItemHelpers
{
    public static IReadOnlyCollection<JoinSelectListItem> SetSelected(
        this IReadOnlyCollection<JoinSelectListItem> collection, int? id)
    {
        var selectedItem = collection.SingleOrDefault(item => item.Value == id);
        if (selectedItem != null)
        {
            selectedItem.Selected = true;
        }
        return collection;
    }
}
