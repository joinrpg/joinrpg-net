using System.Text;
using System.Text.RegularExpressions;

namespace JoinRpg.Services.Notifications;
/// <summary>
/// Used to parse and apply template
/// </summary>
/// <remarks>
/// Class is not thread safe (used StringBuilder)
/// </remarks>
/// <param name="Template"></param>
internal partial class NotifcationFieldsTemplater(NotificationEventTemplate Template)
{
    private readonly string template = Template.TemplateContents;
    private readonly MatchCollection matchCollection = FieldPlaceholderRegex().Matches(Template.TemplateContents);
    private readonly StringBuilder stringBuilder = new(Template.TemplateContents.Length + 50);

    public string[] GetFields() => [.. matchCollection.Select(m => ExtractFieldName(m.Value)).Distinct().Order()];

    internal MarkdownString Substitute(IReadOnlyDictionary<string, string> fields)
    {
        var lastChar = 0;
        foreach (var match in matchCollection.OrderBy(x => x.Index))
        {
            _ = stringBuilder
                .Append(template[lastChar..match.Index])
                .Append(fields[ExtractFieldName(match.Value)]);
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
