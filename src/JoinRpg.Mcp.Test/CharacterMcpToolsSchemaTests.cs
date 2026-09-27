using System.Reflection;
using System.Text.Json;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace JoinRpg.Mcp.Test;

/// <summary>
/// Схема, которую сервер публикует клиенту в <c>tools/list</c>. Клиент не может передать
/// параметр, которого в схеме нет, поэтому потерянный параметр выглядит не ошибкой, а пустым
/// ответом — и причину приходится искать вслепую.
/// </summary>
/// <remarks>
/// Схема строится <b>с Autofac-контейнером</b>, как в Portal, и это здесь главное. SDK
/// исключает из схемы параметры, которые может выдать контейнер, а Autofac неявно разрешает
/// массивы и интерфейсные коллекции: <c>GetService(typeof(int[]))</c> возвращает пустой массив.
/// Из-за этого <c>int[] characterIds</c> пропадал из схемы, сервер получал пустоту, а клиент
/// даже не видел, что параметр существует.
///
/// Первая версия этого теста строила схему без провайдера и потому оставалась зелёной при
/// полностью нерабочем инструменте.
/// </remarks>
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
        // Массив id — единственный способ забрать нескольких персонажей одним вызовом, поэтому
        // проверяем и тип элементов, а не только то, что это массив.
        var schema = BuildSchema(nameof(CharacterMcpTools.GetCharacters));

        var items = schema.GetProperty("properties").GetProperty("characterIds").GetProperty("items");
        items.GetProperty("type").GetString().ShouldBe("integer");
    }

    /// <summary>
    /// Страж на саму причину: если у параметра снова окажется тип, который Autofac умеет
    /// разрешить, он тихо исчезнет из схемы. Тест ловит это на уровне типа, не дожидаясь
    /// жалоб от клиента.
    /// </summary>
    [Fact]
    public void ToolParameterTypes_AreNotResolvableByContainer()
    {
        var container = BuildAutofacProvider();

        foreach (var method in ToolMethods())
        {
            foreach (var parameter in method.GetParameters())
            {
                container.GetService(parameter.ParameterType)
                    .ShouldBeNull(
                        $"{method.Name}: контейнер умеет выдать {parameter.ParameterType.Name} "
                        + $"для параметра '{parameter.Name}', значит SDK уберёт его из схемы");
            }
        }
    }

    private static IEnumerable<MethodInfo> ToolMethods() =>
        typeof(CharacterMcpTools)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes(typeof(McpServerToolAttribute), inherit: false).Length > 0);

    private static AutofacServiceProvider BuildAutofacProvider()
    {
        var builder = new ContainerBuilder();
        builder.Populate(new ServiceCollection());
        return new AutofacServiceProvider(builder.Build());
    }

    private static JsonElement BuildSchema(string methodName)
    {
        var method = typeof(CharacterMcpTools).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Метод {methodName} не найден");

        // Экземпляр нужен только чтобы SDK согласился построить схему — вызывать инструмент мы
        // не будем, поэтому зависимости фиктивные.
        var tool = McpServerTool.Create(
            method,
            new CharacterMcpTools(null!, null!, null!),
            new McpServerToolCreateOptions { Services = BuildAutofacProvider() });

        return JsonSerializer.SerializeToElement(tool.ProtocolTool.InputSchema);
    }
}
