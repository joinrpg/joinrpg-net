namespace JoinRpg.Helpers;

/// <summary>
/// Extensions for <see cref="IEnumerable{T}"/>
/// </summary>
public static class EnumerableExtensions
{

    /// <summary>
    /// Appends item to source enumerable if condition is true
    /// </summary>
    /// <typeparam name="T">Enumerable items type</typeparam>
    /// <param name="self"></param>
    /// <param name="condition">Condition to test</param>
    /// <param name="getItem">Called to get item to append</param>
    public static IEnumerable<T> AppendIf<T>(this IEnumerable<T> self, bool condition, Func<T> getItem)
        => condition ? self.Append(getItem()) : self;

}
