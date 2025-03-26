namespace JoinRpg.PrimitiveTypes;

internal static class IdentificationParseHelper
{
    internal static (int i1, int i2, int i3)? TryParse3(ReadOnlySpan<char> value, IFormatProvider? provider, params ReadOnlySpan<string> prefixes)
    {
        var val = value.Trim();
        foreach (var prefix in prefixes)
        {
            if (val.StartsWith(prefix))
            {
                val = val[prefix.Length..];
            }
        }
        if (val.StartsWith("("))
        {
            val = val[1..];
        }
        if (val.EndsWith(")"))
        {
            val = val[..^1];
        }

        Span<Range> ranges = stackalloc Range[3];
        var count = val.Split(ranges, "-", StringSplitOptions.TrimEntries);
        if (count == 3
           && ProjectIdentification.TryParse(val[ranges[0]], provider, out var i1)
           && int.TryParse(val[ranges[1]], provider, out var i2)
           && int.TryParse(val[ranges[2]], provider, out var i3)
           )
        {
            return (i1, i2, i3);
        }

        return null;
    }

    internal static (int i1, int i2)? TryParse2(ReadOnlySpan<char> value, IFormatProvider? provider, params ReadOnlySpan<string> prefixes)
    {
        var val = value.Trim();
        foreach (var prefix in prefixes)
        {
            if (val.StartsWith(prefix))
            {
                val = val[prefix.Length..];
            }
        }
        if (val.StartsWith("("))
        {
            val = val[1..];
        }
        if (val.EndsWith(")"))
        {
            val = val[..^1];
        }
        Span<Range> ranges = stackalloc Range[2];
        var count = val.Split(ranges, "-", StringSplitOptions.TrimEntries);
        if (count == 2
           && ProjectIdentification.TryParse(val[ranges[0]], provider, out var i1)
           && int.TryParse(val[ranges[1]], provider, out var i2)
           )
        {
            return (i1, i2);
        }

        return null;
    }

    internal static int? TryParse1(ReadOnlySpan<char> value, IFormatProvider? provider, params ReadOnlySpan<string> prefixes)
    {
        var val = value.Trim();
        foreach (var prefix in prefixes)
        {
            if (val.StartsWith(prefix))
            {
                val = val[prefix.Length..];
            }
        }
        if (val.StartsWith("("))
        {
            val = val[1..];
        }
        if (val.EndsWith(")"))
        {
            val = val[..^1];
        }

        if (int.TryParse(val, provider, out var id))
        {
            return id;
        }

        return null;
    }
}
