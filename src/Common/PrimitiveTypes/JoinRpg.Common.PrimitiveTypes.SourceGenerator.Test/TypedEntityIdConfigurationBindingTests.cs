using Microsoft.Extensions.Configuration;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.PrimitiveTypes.SourceGenerator.Test;

/// <summary>
/// Биндер конфигурации не знает про <see cref="IParsable{TSelf}"/>, ему нужен TypeConverter. Без него Id в опциях
/// молча оставался null, и приходилось хранить число и собирать Id руками.
/// </summary>
public class TypedEntityIdConfigurationBindingTests
{
    private const string Source = """
        using JoinRpg.Common.PrimitiveTypes;

        namespace TestNs;

        [TypedEntityId(ShortName = "Project")]
        public partial record ProjectIdentification(int Value);

        [TypedEntityId]
        public partial record FolderIdentification(ProjectIdentification ProjectId, int FolderId);

        public class TestOptions
        {
            public ProjectIdentification? Project { get; set; }
            public FolderIdentification? Folder { get; set; }
        }
        """;

    private static readonly Lazy<Type> OptionsType = new(() =>
    {
        var result = GeneratorTestHelper.RunGenerator(Source);
        result.Errors.ShouldBeEmpty();
        var assembly = GeneratorTestHelper.EmitAndLoad(result.OutputCompilation);
        // [TypeConverter] хранит тип строкой, а сборку, загруженную из байтов, Type.GetType по имени не найдёт.
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) => args.Name == assembly.FullName ? assembly : null;
        return assembly.GetType("TestNs.TestOptions")!;
    });

    private static object? Bind(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build().Get(OptionsType.Value);

    private static string? Read(object? options, string property)
        => OptionsType.Value.GetProperty(property)!.GetValue(options)?.ToString();

    [Theory]
    [InlineData("5")]
    [InlineData("Project(5)")]
    public void Simple_id_binds_from_string(string value)
        => Read(Bind(new() { ["Project"] = value }), "Project").ShouldBe("Project(5)");

    [Fact]
    public void Composite_id_binds_from_string()
        => Read(Bind(new() { ["Folder"] = "5-7" }), "Folder").ShouldBe("FolderId(5-7)");

    [Fact]
    public void Missing_value_stays_null()
        => Read(Bind(new() { ["Folder"] = "5-7" }), "Project").ShouldBeNull();

    [Fact]
    public void Invalid_value_fails_binding()
        => Should.Throw<InvalidOperationException>(() => Bind(new() { ["Project"] = "abc" }));
}
