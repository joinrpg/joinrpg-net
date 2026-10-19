using System.Text;

namespace JoinRpg.Tools.Drp.Common;

/// <summary>
/// CSV DRP-отчётов: разделитель «;», UTF-8 с BOM, перевод строки CRLF — чтобы Excel с русской
/// локалью открыл файл без мастера импорта. Значения с «;», кавычками и переводами строк — в кавычках.
/// Отчёт, который правит человек и читает следующий инструмент, читается этим же классом.
/// </summary>
public static class DrpCsv
{
    public const char Separator = ';';

    public static readonly Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    // Чтение строгое: файл, пересохранённый в ANSI (cp1251), даст ошибку, а не кракозябры в названиях.
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static string Format(IEnumerable<IReadOnlyList<string?>> rows)
    {
        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            for (var i = 0; i < row.Count; i++)
            {
                if (i > 0)
                {
                    _ = sb.Append(Separator);
                }
                _ = sb.Append(Escape(row[i]));
            }
            _ = sb.Append("\r\n");
        }
        return sb.ToString();
    }

    public static Task WriteFile(string path, IEnumerable<IReadOnlyList<string?>> rows, CancellationToken ct)
        => File.WriteAllTextAsync(path, Format(rows), Encoding, ct);

    /// <summary>
    /// Читает файл в UTF-8 (BOM необязателен). Файл не в UTF-8 — <see cref="DrpCsvException"/>.
    /// </summary>
    public static async Task<string> ReadFile(string path, CancellationToken ct)
    {
        var bytes = await File.ReadAllBytesAsync(path, ct);
        var offset = bytes.AsSpan().StartsWith(Encoding.Preamble) ? Encoding.Preamble.Length : 0;
        try
        {
            return StrictUtf8.GetString(bytes, offset, bytes.Length - offset);
        }
        catch (DecoderFallbackException)
        {
            throw new DrpCsvException([$"{path}: файл не в UTF-8. Сохраните его как «CSV UTF-8 (разделитель — точка с запятой)»."]);
        }
    }

    /// <summary>
    /// Текст, который ввёл пользователь (название, адрес): Excel исполняет ячейку, начинающуюся с =, +, -, @
    /// (а также с табуляции или перевода строки перед ними), как формулу, поэтому такие значения
    /// экранируем апострофом. Обратное — <see cref="FromUserText"/>.
    /// </summary>
    public static string? UserText(string? value)
        => value is { Length: > 0 } && IsFormulaStart(value[0]) ? "'" + value : value;

    /// <summary>
    /// Снимает апостроф, поставленный <see cref="UserText"/>.
    /// </summary>
    public static string? FromUserText(string? value)
        => value is { Length: > 1 } && value[0] == '\'' && IsFormulaStart(value[1]) ? value[1..] : value;

    private static bool IsFormulaStart(char c) => c is '=' or '+' or '-' or '@' or '\t' or '\r';

    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }
        return value.IndexOfAny([Separator, '"', '\r', '\n']) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }

    /// <summary>
    /// Разбирает CSV в строки. Пустые строки пропускаются.
    /// </summary>
    /// <returns>Строки с номером строки файла, на которой запись начинается (с 1).</returns>
    public static IReadOnlyList<(int Line, IReadOnlyList<string> Values)> Parse(string text)
    {
        var result = new List<(int, IReadOnlyList<string>)>();
        var values = new List<string>();
        var value = new StringBuilder();
        var line = 1;
        var recordLine = 1;
        var inQuotes = false;
        var valueStarted = false;

        void EndValue()
        {
            values.Add(value.ToString());
            _ = value.Clear();
            valueStarted = false;
        }

        void EndRecord()
        {
            EndValue();
            if (values.Count > 1 || values[0].Length > 0)
            {
                result.Add((recordLine, values.ToArray()));
            }
            values.Clear();
        }

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        _ = value.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    if (c == '\n')
                    {
                        line++;
                    }
                    _ = value.Append(c);
                }
                continue;
            }

            switch (c)
            {
                case '"' when !valueStarted:
                    inQuotes = true;
                    valueStarted = true;
                    break;
                case Separator:
                    EndValue();
                    break;
                case '\r' when i + 1 < text.Length && text[i + 1] == '\n':
                    break;
                case '\r' or '\n':
                    EndRecord();
                    line++;
                    recordLine = line;
                    break;
                default:
                    _ = value.Append(c);
                    valueStarted = true;
                    break;
            }
        }

        if (inQuotes)
        {
            throw new DrpCsvException([$"Строка {recordLine.ToString(System.Globalization.CultureInfo.InvariantCulture)}: не закрыта кавычка."]);
        }
        if (valueStarted || values.Count > 0)
        {
            EndRecord();
        }
        return result;
    }
}

public sealed class DrpCsvException(IReadOnlyList<string> errors)
    : Exception(string.Join(Environment.NewLine, errors))
{
    public IReadOnlyList<string> Errors { get; } = [.. errors];
}
