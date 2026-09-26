using System.Data.SqlClient;
using System.Text;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// docs/db-schema.md заявлен как схема «со всеми полями», но поддерживается руками:
/// автогенератора mermaid из EF6-модели нет, а ценность документа как раз в комментариях,
/// которых в схеме нет. Поэтому расхождение ловит тест, а правит человек или агент —
/// см. docs/db-schema-regenerate.md.
/// <para>
/// Тест интеграционный, потому что сверяется не с C#-моделью, а с реальной схемой: база
/// поднимается в контейнере и накатывается миграциями, дальше сравниваются таблицы и колонки
/// из INFORMATION_SCHEMA с разобранными mermaid-блоками документа.
/// </para>
/// </summary>
public class DbSchemaDocumentationScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    /// <summary>
    /// Таблицы, которые есть в базе, но в документе не показываются.
    /// </summary>
    private static readonly HashSet<string> TablesNotDocumented = new(StringComparer.OrdinalIgnoreCase)
    {
        // Служебная таблица EF6, частью предметной схемы не является.
        "__MigrationHistory",

        // Мёртвый механизм плагинов: код удалён, таблицы остались.
        "ProjectPlugins",
        "PluginFieldMappings",
    };

    [Fact]
    public async Task DocumentedSchema_MatchesDatabase()
    {
        var documented = DbSchemaDoc.Read();
        var actual = await LoadSchemaAsync();

        var problems = new StringBuilder();

        var missingTables = actual.Keys
            .Where(t => !documented.ContainsKey(t) && !TablesNotDocumented.Contains(t))
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase);
        foreach (var table in missingTables)
        {
            _ = problems.AppendLine($"  таблица {table}: есть в базе, нет в документе");
        }

        var extraTables = documented.Keys
            .Where(t => !actual.ContainsKey(t))
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase);
        foreach (var table in extraTables)
        {
            _ = problems.AppendLine($"  таблица {table}: есть в документе, нет в базе");
        }

        foreach (var (table, actualColumns) in actual.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!documented.TryGetValue(table, out var documentedColumns))
            {
                continue; // уже сообщили про таблицу целиком
            }

            foreach (var column in actualColumns.Except(documentedColumns, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
            {
                _ = problems.AppendLine($"  {table}.{column}: есть в базе, нет в документе");
            }

            foreach (var column in documentedColumns.Except(actualColumns, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
            {
                _ = problems.AppendLine($"  {table}.{column}: есть в документе, нет в базе");
            }
        }

        problems.Length.ShouldBe(
            0,
            $"""
            docs/db-schema.md разошёлся с реальной схемой базы.
            Как обновить документ — docs/db-schema-regenerate.md.

            {problems}
            """);
    }

    private async Task<Dictionary<string, HashSet<string>>> LoadSchemaAsync()
    {
        var schema = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        using var connection = new SqlConnection(factory.ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.TABLE_NAME, c.COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS c
            JOIN INFORMATION_SCHEMA.TABLES t
                ON t.TABLE_SCHEMA = c.TABLE_SCHEMA AND t.TABLE_NAME = c.TABLE_NAME
            WHERE t.TABLE_TYPE = 'BASE TABLE'
            """;

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var table = reader.GetString(0);
            if (!schema.TryGetValue(table, out var columns))
            {
                columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                schema[table] = columns;
            }

            _ = columns.Add(reader.GetString(1));
        }

        schema.ShouldNotBeEmpty("Не нашли ни одной таблицы — база не накатилась?");
        return schema;
    }
}
