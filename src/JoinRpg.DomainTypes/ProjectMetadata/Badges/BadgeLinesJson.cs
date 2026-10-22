using System.Text.Encodings.Web;
using System.Text.Json;

namespace JoinRpg.DomainTypes.ProjectMetadata.Badges;

/// <summary>
/// Формат хранения строк бейджа в БД (колонка <c>LinesJson</c>). Строки всегда правятся целиком
/// вместе с шаблоном, поэтому отдельной таблицы под них нет.
/// </summary>
public static class BadgeLinesJson
{
    // Кириллицу пишем как есть, а не \uXXXX: так строки читаются в БД глазами. HTML-экранирование
    // здесь не нужно — в разметку значения попадают через Razor, который экранирует сам.
    private static readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Serialize(IReadOnlyList<BadgeLine> lines) => JsonSerializer.Serialize(lines, options);

    public static IReadOnlyList<BadgeLine> Deserialize(string json)
        => JsonSerializer.Deserialize<BadgeLine[]>(json, options) ?? [];
}
