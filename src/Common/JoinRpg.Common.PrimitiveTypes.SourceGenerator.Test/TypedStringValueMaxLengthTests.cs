using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.PrimitiveTypes.SourceGenerator.Test;

/// <summary>
/// Регрессия: у <c>MarkdownString</c> не был задан <c>MaxLength</c>, и применялся дефолт в
/// 999 символов. Для длинных текстов (markdown) ограничения быть не должно, признак этого —
/// <see cref="int.MaxValue"/>: генератор не должен выпускать проверку длины вовсе.
/// </summary>
public class TypedStringValueMaxLengthTests
{
    private const string LengthCheckMarker = "не длиннее";

    [Fact]
    public void MaxLengthIntMaxValue_NoLengthCheckGenerated()
    {
        var generated = Generate("MaxLength = int.MaxValue, MinLength = 0, Trim = false");

        generated.ShouldNotContain(LengthCheckMarker);
    }

    [Fact]
    public void DefaultMaxLength_LengthCheckIsGenerated()
    {
        var generated = Generate("");

        generated.ShouldContain(LengthCheckMarker);
        generated.ShouldContain("999");
    }

    private static string Generate(string attributeArguments)
    {
        var source = $$"""
            namespace JoinRpg.Common.PrimitiveTypes;

            [TypedStringValue({{attributeArguments}})]
            public sealed partial record SampleString(string Value);
            """;

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(TypedStringValueAttribute).Assembly.Location))
            .ToList();

        var compilation = CSharpCompilation.Create(
            assemblyName: "TypedStringValueGeneratorTestAssembly",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generatedTrees = CSharpGeneratorDriver
            .Create(new TypedStringValueGenerator())
            .RunGenerators(compilation)
            .GetRunResult()
            .GeneratedTrees;

        return generatedTrees.ShouldHaveSingleItem().ToString();
    }
}
