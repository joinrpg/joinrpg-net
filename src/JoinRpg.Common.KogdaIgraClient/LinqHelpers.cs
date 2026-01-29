namespace JoinRpg.Common.KogdaIgraClient;

internal static class LinqHelpers
{
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
}
