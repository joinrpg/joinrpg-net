namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Разбор mermaid-блоков из docs/db-schema.md: какие таблицы и колонки там описаны.
/// </summary>
internal static class DbSchemaDoc
{
    private const string DocRelativePath = "docs/db-schema.md";

    /// <summary>
    /// Заголовок, после которого в документе идут другие базы (PostgreSQL) — их таблицы
    /// в основной схеме искать не надо.
    /// </summary>
    private const string OtherDatabasesHeading = "## Отдельные БД";

    /// <summary>
    /// Таблица → её колонки, как они описаны в документе.
    /// </summary>
    public static Dictionary<string, HashSet<string>> Read()
    {
        var tables = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        HashSet<string>? current = null;
        var insideMermaid = false;

        foreach (var raw in File.ReadLines(FindDoc()))
        {
            var line = raw.TrimEnd();

            if (line.StartsWith(OtherDatabasesHeading, StringComparison.Ordinal))
            {
                break;
            }

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                insideMermaid = line.StartsWith("```mermaid", StringComparison.Ordinal);
                current = null;
                continue;
            }

            if (!insideMermaid)
            {
                continue;
            }

            // Начало блока таблицы: "    Projects {"
            if (line.EndsWith('{'))
            {
                var name = line[..^1].Trim();
                if (!tables.TryGetValue(name, out current))
                {
                    current = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    tables[name] = current;
                }

                continue;
            }

            if (line.Trim() == "}")
            {
                current = null;
                continue;
            }

            if (current is null)
            {
                continue;
            }

            // Строка атрибута: "        int ProjectId PK "комментарий"" — тип, имя, дальше не важно.
            var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                _ = current.Add(parts[1]);
            }
        }

        tables.ShouldNotBeEmpty($"Не разобрали ни одной таблицы из {DocRelativePath} — сломался парсер или формат документа");
        return tables;
    }

    /// <summary>
    /// Тесты запускаются из bin/, поэтому корень репозитория ищем вверх по дереву.
    /// </summary>
    private static string FindDoc()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, DocRelativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Не нашли {DocRelativePath} вверх от {AppContext.BaseDirectory}");
    }
}
