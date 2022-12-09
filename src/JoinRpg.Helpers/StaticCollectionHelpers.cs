namespace JoinRpg.Helpers;

public static class StaticCollectionHelpers
{
    public static ISet<T> FlatTree<T>(this T obj,
        Func<T, IEnumerable<T>> parentSelectorFunc,
        bool includeSelf = true)
        where T : IEquatable<T>
    {
        var groups = new HashSet<T>();
        if (obj == null)
        {
            return groups;
        }

        if (includeSelf)
        {
            _ = groups.Add(obj);
        }

        FlatTreeImpl(groups, obj, parentSelectorFunc);
        return groups;
    }

    private static void FlatTreeImpl<T>(ISet<T> flattedcharacterGroupIds,
        T group1,
        Func<T, IEnumerable<T>> parentSelectorFunc)
    {
        foreach (var characterGroup in parentSelectorFunc(group1)
            .Except(flattedcharacterGroupIds))
        {
            FlatTreeImpl(flattedcharacterGroupIds, characterGroup, parentSelectorFunc);
            _ = flattedcharacterGroupIds.Add(characterGroup);
        }
    }

    public static IEnumerable<T> UnionIf<T>(this IEnumerable<T> source,
        IEnumerable<T> enumerable,
        bool add)
        => source.Union(add ? enumerable : Enumerable.Empty<T>());

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
        where T : class
    {
        foreach (var i in source)
        {
            if (i is T v)
            {
                yield return v;
            }
        }
    }

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
        where T : struct
    {
        foreach (var i in source)
        {
            if (i is T v)
            {
                yield return v;
            }
        }
    }

    public static IEnumerable<T> OrEmptyList<T>(this IEnumerable<T>? collection) => collection ?? Enumerable.Empty<T>();

    public static IEnumerable<T> UnionUntilTotalCount<T>(
        this IReadOnlyCollection<T> alreadyTaken,
        IEnumerable<T> toAdd,
        int totalLimit)
    {
        ArgumentNullException.ThrowIfNull(alreadyTaken);
        ArgumentNullException.ThrowIfNull(toAdd);

        return alreadyTaken.Union(
            toAdd
                .Except(alreadyTaken)
                .Take(Math.Max(0, totalLimit - alreadyTaken.Count)));
    }
}
