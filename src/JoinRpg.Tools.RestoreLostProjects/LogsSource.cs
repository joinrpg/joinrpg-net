using System.Globalization;
using System.Text;
using System.Text.Json;
using JoinRpg.DomainTypes;

namespace JoinRpg.Tools.RestoreLostProjects;

/// <summary>
/// Сохранённые логи k8s (выгрузка <c>yc logging read --format json</c>). Принимает и JSON-массив,
/// и по объекту на строку. Поля Serilog ищутся в <c>json_payload</c>, а если его нет — в самой записи.
/// </summary>
/// <remarks>
/// Одна и та же запись может лежать в нескольких файлах выгрузки (сырые окна и объединённый файл),
/// поэтому записи с одинаковым <c>uid</c> (он есть у каждой записи yc logging read) считаются один раз.
/// Учитываются только ответы ниже 400: запрос с ошибкой (404 на чужой или несуществующий id, отказ
/// в доступе) ничего в проекте не менял и не говорит ни о проекте, ни о его создателе. 302 — обычный
/// исход успешного POST. Запись без StatusCode учитывается.
/// </remarks>
internal static class LogsSource
{
    public static async Task<IReadOnlyList<LogEntry>> Read(IEnumerable<string> paths, DateTimeOffset lostAt, CancellationToken ct)
    {
        var result = new List<LogEntry>();
        var seenUids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            await foreach (var element in ReadElements(path, ct))
            {
                if (Parse(element) is not { } entry || entry.Timestamp >= lostAt || entry.StatusCode >= 400)
                {
                    continue;
                }
                if (element.ValueKind == JsonValueKind.Object && GetString(element, "uid") is { } uid && !seenUids.Add(uid))
                {
                    continue;
                }
                result.Add(entry);
            }
        }
        return result;
    }

    private static async IAsyncEnumerable<JsonElement> ReadElements(string path, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        if (await StartsWithArray(stream, ct))
        {
            await foreach (var element in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(stream, cancellationToken: ct))
            {
                yield return element;
            }
            yield break;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                using var document = JsonDocument.Parse(line);
                yield return document.RootElement.Clone();
            }
        }
    }

    private static async Task<bool> StartsWithArray(Stream stream, CancellationToken ct)
    {
        var buffer = new byte[1];
        var result = false;
        while (await stream.ReadAsync(buffer, ct) == 1)
        {
            // BOM (EF BB BF) и пробелы пропускаем.
            if (buffer[0] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n' or 0xEF or 0xBB or 0xBF)
            {
                continue;
            }
            result = buffer[0] == (byte)'[';
            break;
        }
        stream.Position = 0;
        return result;
    }

    /// <summary>
    /// Разбирает одну запись. Запись без проекта и пути бесполезна — null.
    /// </summary>
    internal static LogEntry? Parse(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }
        var payload = element.TryGetProperty("json_payload", out var p) && p.ValueKind == JsonValueKind.Object ? p : element;

        var entry = new LogEntry(
            ParseProjectId(Get(payload, "ProjectId")),
            GetString(payload, "LoggedUser"),
            GetString(payload, "RequestPath"),
            GetString(payload, "ActionName"),
            ParseTimestamp(GetString(payload, "@timestamp") ?? GetString(element, "timestamp")),
            Get(payload, "StatusCode") is { ValueKind: JsonValueKind.Number } status && status.TryGetInt32(out var code) ? code : null);
        return entry.ProjectId is null && entry.RequestPath is null ? null : entry;
    }

    /// <summary>
    /// ProjectId бывает числом (1535 — в логе запроса) и строкой «Project(158)» (в scope-полях);
    /// строку разбирает доменный ProjectIdentification.
    /// </summary>
    internal static int? ParseProjectId(JsonElement? value)
    {
        var id = value switch
        {
            { ValueKind: JsonValueKind.Number } number => number.TryGetInt32(out var n) ? n : (int?)null,
            { ValueKind: JsonValueKind.String } str
                => ProjectIdentification.TryParse(str.GetString(), null, out var project) ? project.Value : null,
            _ => null,
        };
        return id > 0 ? id : null;
    }

    private static DateTimeOffset? ParseTimestamp(string? value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result) ? result : null;

    private static JsonElement? Get(JsonElement obj, string name)
        => obj.TryGetProperty(name, out var value) ? value : null;

    private static string? GetString(JsonElement obj, string name)
        => obj.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } s ? s : null;
}
