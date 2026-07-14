namespace JoinRpg.Web.ProjectCommon.Fields;

[Obsolete("Legacy MVC moveable list pattern. Do not use in new code.")]
public interface IMoveableNonInteractiveListItem
{
    bool First { get; set; }
    bool Last { get; set; }
    int ProjectId { get; }
    int ItemId { get; }
}

public static class MoveableNonInteractiveListItemExtensions
{
#pragma warning disable CS0618
    public static IList<T> MarkFirstAndLast<T>(this IList<T> collection) where T : IMoveableNonInteractiveListItem
    {
        if (collection.Any())
        {
            collection.First().First = true;
            collection.Last().Last = true;
        }
        return collection;
    }

    public static IList<T> MarkFirstAndLast<T>(this IEnumerable<T> collection) where T : IMoveableNonInteractiveListItem
        => collection.ToArray().MarkFirstAndLast();
#pragma warning restore CS0618
}
