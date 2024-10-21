using System.Text;
using System.Text.RegularExpressions;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Notifications;
/// <summary>
/// Used to parse and apply template
/// </summary>
/// <remarks>
/// Class is not thread safe (used StringBuilder)
/// </remarks>
/// <param name="Template"></param>
internal partial class NotifcationFieldsTemplater(MarkdownString Template)
{
    private readonly string template = Template.Contents!;
    private readonly MatchCollection matchCollection = FieldPlaceholderRegex().Matches(Template.Contents!);
    private readonly StringBuilder stringBuilder = new(Template.Contents!.Length + 50);

    public string[] GetFields() => [.. matchCollection.Select(m => ExtractFieldName(m.Value)).Distinct().Order()];

    internal MarkdownString Substitute(IReadOnlyDictionary<string, string> fields, UserDisplayName displayName)
    {
        var dict = new Dictionary<string, string>(fields)
        {
            { "name", displayName.DisplayName }
        };

        var lastChar = 0;
        foreach (var match in matchCollection.OrderBy(x => x.Index))
        {
            _ = stringBuilder
                .Append(template[lastChar..match.Index])
                .Append(dict[ExtractFieldName(match.Value)]);
            lastChar = match.Index + match.Length;
        }
        var result = new MarkdownString(stringBuilder.Append(template[lastChar..]).ToString());
        _ = stringBuilder.Clear();
        return result;
    }

    private static string ExtractFieldName(string value) => value["%recepient.".Length..^1];

    [GeneratedRegex("%recepient\\.(\\w+?)%", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FieldPlaceholderRegex();
}
