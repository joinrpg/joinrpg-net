using System.Reflection;
using System.Text.Json;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Mcp;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Схема инструментов, которую сервер публикует клиенту, — на живом хосте Portal с его
/// настоящим Autofac-контейнером и настоящей регистрацией MCP.
/// </summary>
/// <remarks>
/// Контейнер здесь принципиален. SDK исключает из схемы параметры, которые способен выдать
/// контейнер, а Autofac неявно разрешает массивы и интерфейсные коллекции как «все регистрации
/// элемента»: <c>GetService(typeof(int[]))</c> возвращает пустой массив. Из-за этого
/// <c>int[] characterIds</c> исчезал из схемы, клиент не мог его передать, а сервер получал
/// пустоту.
///
/// Прошлые версии этого теста строили схему сначала вовсе без контейнера, потом с пустым
/// Autofac, собранным в тесте. Оба варианта — подделка: в Portal зарегистрированы модули, и
/// параметр, разрешимый там, но не в пустом контейнере, снова прошёл бы незамеченным. Поэтому
/// схема берётся из того же <see cref="McpServerOptions"/>, который отдаёт живой хост.
/// </remarks>
public class McpToolSchemaTests : IAsyncLifetime
{
    private readonly McpEnabledApplicationFactory factory = new();

    public Task InitializeAsync() => ((IAsyncLifetime)factory).InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)factory).DisposeAsync();

    [Theory]
    [InlineData("list_characters", "projectId", "integer")]
    [InlineData("list_characters", "groupId", "integer")]
    [InlineData("get_characters", "projectId", "integer")]
    [InlineData("get_characters", "characterIds", "array")]
    [InlineData("search_characters", "projectId", "integer")]
    [InlineData("search_characters", "query", "string")]
    [InlineData("get_project_overview", "projectId", "integer")]
    public void Parameter_IsPublishedRequiredAndTyped(string toolName, string parameter, string expectedType)
    {
        var schema = SchemaOf(toolName);

        var properties = schema.GetProperty("properties");
        properties.TryGetProperty(parameter, out var property)
            .ShouldBeTrue($"параметра '{parameter}' нет в схеме {toolName} — клиент не сможет его передать");
        property.GetProperty("type").GetString().ShouldBe(expectedType);

        schema.GetProperty("required").EnumerateArray().Select(x => x.GetString())
            .ShouldContain(parameter, $"параметр '{parameter}' у {toolName} должен быть обязательным");
    }

    [Fact]
    public void GetCharacters_CharacterIds_IsArrayOfIntegers()
    {
        // Массив id — единственный способ забрать нескольких персонажей одним вызовом, поэтому
        // проверяется и тип элементов.
        var items = SchemaOf("get_characters")
            .GetProperty("properties").GetProperty("characterIds").GetProperty("items");

        items.GetProperty("type").GetString().ShouldBe("integer");
    }

    /// <summary>
    /// Страж на саму причину, через <b>живой</b> контейнер Portal: если контейнер способен выдать
    /// тип параметра, SDK молча уберёт этот параметр из схемы, и клиент не сможет его передать.
    /// </summary>
    /// <remarks>
    /// Проверка идёт по сигнатурам методов, а не по схеме, поэтому ловит и инструмент без
    /// параметров, и новый инструмент с неудачным типом — не дожидаясь жалоб от клиента.
    /// Контейнер именно настоящий: в пустом контейнере, собранном в тесте, разрешались бы только
    /// коллекции, а зарегистрированные в Portal типы прошли бы незамеченными.
    /// </remarks>
    [Theory]
    [InlineData(typeof(CharacterMcpTools))]
    [InlineData(typeof(ProjectMcpTools))]
    public void ToolParameterTypes_AreNotResolvableByLiveContainer(Type toolType)
    {
        var methods = toolType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes(typeof(McpServerToolAttribute), inherit: false).Length > 0)
            .ToList();

        methods.ShouldNotBeEmpty($"у {toolType.Name} не нашлось методов с [McpServerTool]");

        using var scope = factory.Services.CreateScope();

        foreach (var method in methods)
        {
            foreach (var parameter in method.GetParameters())
            {
                scope.ServiceProvider.GetService(parameter.ParameterType).ShouldBeNull(
                    $"{toolType.Name}.{method.Name}: контейнер умеет выдать "
                    + $"{parameter.ParameterType.Name} для параметра '{parameter.Name}', "
                    + "значит SDK уберёт его из схемы и клиент не сможет его передать");
            }
        }
    }

    private JsonElement SchemaOf(string toolName)
    {
        var tool = PublishedTools().SingleOrDefault(t => t.ProtocolTool.Name == toolName)
            ?? throw new InvalidOperationException(
                $"Инструмент '{toolName}' не опубликован. Есть: "
                + string.Join(", ", PublishedTools().Select(t => t.ProtocolTool.Name)));

        return JsonSerializer.SerializeToElement(tool.ProtocolTool.InputSchema);
    }

    /// <summary>То, что живой хост реально отдаёт в tools/list.</summary>
    private IReadOnlyCollection<McpServerTool> PublishedTools()
    {
        var options = factory.Services.GetRequiredService<IOptions<McpServerOptions>>().Value;
        var tools = options.ToolCollection
            ?? throw new InvalidOperationException("MCP-инструменты не зарегистрированы на хосте");

        return [.. tools];
    }
}
