namespace JoinRpg.WebComponents;

public interface IMoveableListItem
{
    string Id { get; }
    string ParentId { get; }
    string DisplayText { get; }
    string Subtext { get; }
}
