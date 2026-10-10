using System.Globalization;
using System.Text;

namespace JoinRpg.Tools.RestoreLostUsers;

/// <summary>
/// CSV-отчёт с решением по каждому пользователю. Содержит персональные данные — хранить только локально.
/// Разделитель «;» и BOM — чтобы Excel с русской локалью открыл файл без мастера импорта.
/// </summary>
internal static class ReportWriter
{
    private static readonly string[] Header =
    [
        "UserId", "Решение", "Причина", "Email","TelegramChatId", "TelegramUserName",
        "ПерваяАктивность", "ПоследняяАктивность", "КакПолучатель", "КакИнициатор", "КонфликтующийUserId", "ВсеEmail",
    ];

    public static string Build(IEnumerable<LostUserDecision> decisions)
    {
        var sb = new StringBuilder();
        AppendRow(sb, Header);
        foreach (var d in decisions)
        {
            AppendRow(sb,
            [
                d.UserId.ToString(CultureInfo.InvariantCulture),
                d.Kind.ToString(),
                d.Reason,
                UserText(d.Email),
                d.TelegramChatId?.ToString(CultureInfo.InvariantCulture),
                UserText(d.TelegramUserName),
                d.FirstSeen.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                d.LastSeen.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                d.AsRecipient.ToString(CultureInfo.InvariantCulture),
                d.AsInitiator.ToString(CultureInfo.InvariantCulture),
                d.ConflictingUserId?.ToString(CultureInfo.InvariantCulture),
                d.AllEmails is null ? null : string.Join(" ", d.AllEmails.Select(UserText)),
            ]);
        }
        return sb.ToString();
    }

    public static Task Write(string path, IEnumerable<LostUserDecision> decisions, CancellationToken ct)
        => File.WriteAllTextAsync(path, Build(decisions), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), ct);

    private static void AppendRow(StringBuilder sb, string?[] values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                _ = sb.Append(';');
            }
            _ = sb.Append(Escape(values[i]));
        }
        _ = sb.Append("\r\n");
    }

    /// <summary>
    /// Текст, который ввёл пользователь (имя, адрес): Excel исполняет ячейку, начинающуюся с =, +, -, @
    /// (а также с табуляции или перевода строки перед ними), как формулу, поэтому такие значения
    /// экранируем апострофом.
    /// </summary>
    internal static string? UserText(string? value)
        => value is { Length: > 0 } && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + value : value;

    internal static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }
        return value.IndexOfAny([';', '"', '\r', '\n']) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }
}
