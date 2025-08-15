using JoinRpg.DataModel;

namespace JoinRpg.Helpers;

public static class StaticStringHelpers
{
    public static string JoinIfNotNullOrWhitespace(
        this IEnumerable<string?> strings,
        string separator) => string.Join(separator, strings.WhereNotNullOrWhiteSpace());

    public static IEnumerable<string> WhereNotNullOrWhiteSpace(
        this IEnumerable<string?> strings)
    {
        ArgumentNullException.ThrowIfNull(strings);

        return strings.Where(s => !string.IsNullOrWhiteSpace(s)).WhereNotNull();
    }

    public static string JoinStrings(
        this IEnumerable<string> strings,
        string separator)
    {
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(separator);

        return string.Join(separator, strings);
    }

    public static string AsString(this IEnumerable<char> strings)
    {
        ArgumentNullException.ThrowIfNull(strings);

        return string.Join("", strings);
    }

    public static IEnumerable<int> UnprefixNumbers(
        this IEnumerable<string> enumerable,
        string prefix)
    {
        return enumerable.Where(key => key.StartsWith(prefix))
            .Select(key => key[prefix.Length..]).Select(int.Parse);
    }

    public static int? TryUnprefixNumber(
        this string number,
        string prefix)
    {
        return number.StartsWith(prefix)
            && int.TryParse(number[prefix.Length..], out var result)
            ? result
            : null;
    }

    public static string RemoveFromString(
        this string str,
        IEnumerable<string> tokensToRemove,
        StringComparison stringComparison = StringComparison.CurrentCulture)
    {
        ArgumentNullException.ThrowIfNull(str);
        ArgumentNullException.ThrowIfNull(tokensToRemove);

        return tokensToRemove.Aggregate(str,
            (current, replaceToken) =>
                current.RemoveFromString(replaceToken, stringComparison));
    }

    public static string RemoveFromString(
        this string str,
        string tokenToRemove,
        StringComparison stringComparison = StringComparison.CurrentCulture)
    {
        ArgumentNullException.ThrowIfNull(str);
        ArgumentNullException.ThrowIfNull(tokenToRemove);

        var retval = str;
        // find the first occurence of oldValue
        var pos = retval.IndexOf(tokenToRemove, stringComparison);

        while (pos > -1)
        {
            // remove oldValue from the string
            retval = retval.Remove(pos, tokenToRemove.Length);

            // check if oldValue is found further down
            pos = retval.IndexOf(tokenToRemove, pos, stringComparison);
        }

        return retval;
    }

    public static List<int> ParseToIntList(this ReadOnlySpan<char> intList)
    {
        var list = new List<int>();
        foreach (var range in intList.Split(','))
        {
            var item = intList[range].Trim();
            if (item.Length == 0)
            {
                continue;
            }
            list.Add(int.Parse(item));
        }
        return list;
    }

    public static List<int> ParseToIntList(this string intList) => intList.AsSpan().ParseToIntList();

    public static string WithDefaultStringValue(
        this string? value,
        string defaultValue)
    {
        ArgumentNullException.ThrowIfNull(defaultValue);

        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }

    public static MarkdownString WithDefaultStringValue(
    this MarkdownString? value,
    string defaultValue)
    {
        ArgumentNullException.ThrowIfNull(defaultValue);

        return (value is null || string.IsNullOrEmpty(value.Contents)) ? new MarkdownString(defaultValue) : value;
    }

    public static string ToHexString(this Guid guid) => Convert.ToHexString(guid.ToByteArray());
}
