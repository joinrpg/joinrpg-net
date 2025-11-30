using System.Diagnostics.CodeAnalysis;
using System.Text;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Models.CommonTypes;
/// <summary>
/// Класс прендназначен для эффективнной компрессии при передаче через веб больших списков Id ников
/// </summary>
/// <param name="List"></param>
public record class CompressedIntList(IReadOnlyCollection<int> List) : ISpanParsable<CompressedIntList>
{
    public CompressedIntList(IEnumerable<IProjectEntityId> list) : this([.. list.Select(c => c.Id)]) { }

    public static CompressedIntList Parse(string s, IFormatProvider? provider = null) => Parse(s.AsSpan(), provider);
    public static CompressedIntList Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => TryParse(s, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(s));

    public static bool TryParse(string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out CompressedIntList result) => TryParse(s.AsSpan(), provider, out result);

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out CompressedIntList result)
    {
        var list = MagicUnJoin(s);
        DeltaUnCompress(list);
        result = new CompressedIntList(list);
        return true;
    }

    public override string ToString() => MagicJoin(DeltaCompress(List.OrderBy(l => l))).ToString();

    public IReadOnlyCollection<CharacterIdentification> ToCharacterIds(ProjectIdentification projectId)
    {
        return [.. List.Select(x => new CharacterIdentification(projectId, x))];
    }

    public IReadOnlyCollection<ClaimIdentification> ToClaimIds(ProjectIdentification projectId)
    {
        return [.. List.Select(x => new ClaimIdentification(projectId, x))];
    }

    private string MagicJoin(IEnumerable<int> list)
    {
        var builder = new StringBuilder(List.Count * 2);
        using (var enumerator = list.GetEnumerator())
        {
            var needSep = false;
            while (enumerator.MoveNext())
            {
                var next = enumerator.Current;
                while (true)
                {
                    if (next == 1)
                    {
                        var count = 1;
                        var b = enumerator.MoveNext();
                        while (b && enumerator.Current == 1 && count < 25)
                        {
                            count++;
                            b = enumerator.MoveNext();
                        }
                        _ = builder.Append((char)('A' + count - 1));
                        if (!b)
                        {
                            break;
                        }
                        next = enumerator.Current;
                        needSep = false;
                        continue;
                    }
                    if (next < 25)
                    {
                        _ = builder.Append((char)('a' + next - 2));
                        needSep = false;
                        break;
                    }

                    if (needSep)
                    {
                        _ = builder.Append(',');
                    }
                    _ = builder.Append(next - 25);
                    needSep = true;
                    break;
                }
            }
        }
        return builder.ToString();
    }

    private static List<int> MagicUnJoin(ReadOnlySpan<char> str)
    {
        var list = new List<int>(str.Length);

        var idx = -1;
        var buffer = "";
        while (idx + 1 < str.Length)
        {
            idx++;
            if (char.IsDigit(str[idx]))
            {
                buffer += str[idx];
                continue;
            }
            if (buffer != "")
            {
                list.Add(int.Parse(buffer) + 25);
                buffer = "";
            }
            if (str[idx] >= 'a' && str[idx] <= 'z')
            {
                list.Add(str[idx] - 'a' + 2);
            }
            if (str[idx] >= 'A' && str[idx] <= 'Z')
            {
                for (var c = 'A'; c <= str[idx]; c++)
                {
                    list.Add(1);
                }
            }
        }
        if (buffer != "")
        {
            list.Add(int.Parse(buffer) + 25);
        }
        return list;
    }

    private static void DeltaUnCompress(List<int> list)
    {
        var prev = 0;
        for (var i = 0; i < list.Count; i++)
        {
            prev += list[i];
            list[i] = prev;
        }
    }

    private static IEnumerable<int> DeltaCompress(IEnumerable<int> list)
    {
        var prev = 0;
        foreach (var i in list)
        {
            yield return i - prev;
            prev = i;
        }
    }
}
