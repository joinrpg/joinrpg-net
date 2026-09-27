using System.Reflection;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace JoinRpg.Mcp.Test;

/// <summary>
/// Схема, которую сервер публикует клиенту в <c>tools/list</c>. Клиент отбрасывает аргументы,
/// которых нет в схеме, поэтому потерянный параметр выглядит не ошибкой, а пустым ответом —
/// и причину приходится искать вслепую. Здесь это утверждение проверяемо.
/// </summary>
public class CharacterMcpToolsSchemaTests
{
    [Theory]
    [InlineData(nameof(CharacterMcpTools.ListCharacters), "groupId", "integer")]
    [InlineData(nameof(CharacterMcpTools.GetCharacters), "characterIds", "array")]
    [InlineData(nameof(CharacterMcpTools.SearchCharacters), "query", "string")]
    public void SecondParameter_IsPublishedAndRequired(string methodName, string parameter, string expectedType)
    {
        var schema = BuildSchema(methodName);

        var properties = schema.GetProperty("properties");
        properties.TryGetProperty(parameter, out var property)
            .ShouldBeTrue($"параметра {parameter} нет в схеме — клиент не сможет его передать");
        property.GetProperty("type").GetString().ShouldBe(expectedType);

        schema.GetProperty("required").EnumerateArray()
            .Select(x => x.GetString())
            .ShouldContain(parameter);
    }

    [Fact]
    public void GetCharacters_DeclaresArrayOfIntegers()
    {
        // Массив id — единственный способ забрать нескольких персонажей одним вызовом,
        // поэтому проверяем и тип элементов, а не только то, что это массив.
        var schema = BuildSchema(nameof(CharacterMcpTools.GetCharacters));

        var items = schema.GetProperty("properties").GetProperty("characterIds").GetProperty("items");
        items.GetProperty("type").GetString().ShouldBe("integer");
    }

    private static JsonElement BuildSchema(string methodName)
    {
        var method = typeof(CharacterMcpTools).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Метод {methodName} не найден");

        // Экземпляр нужен только чтобы SDK согласился построить схему — вызывать инструмент
        // мы не будем, поэтому зависимости фиктивные.
        var tool = McpServerTool.Create(method, new CharacterMcpTools(null!, null!, null!));

        return JsonSerializer.SerializeToElement(tool.ProtocolTool.InputSchema);
    }
}
