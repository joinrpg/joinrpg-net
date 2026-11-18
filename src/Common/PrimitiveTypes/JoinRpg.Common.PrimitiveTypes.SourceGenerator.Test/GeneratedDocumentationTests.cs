using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.PrimitiveTypes.SourceGenerator.Test;

/// <summary>
/// Сгенерированные публичные члены документированы: компилируем результат в режиме
/// <see cref="DocumentationMode.Diagnose"/> и требуем, чтобы в нём не было CS15xx — ни отсутствующих
/// комментариев, ни битых param/cref.
/// </summary>
public class GeneratedDocumentationTests
{
    private const string Source = """
        using JoinRpg.Common.PrimitiveTypes;

        namespace TestNs;

        [TypedEntityId(ShortName = "Project")]
        public partial record ProjectIdentification(int Value);

        [TypedEntityId(ShortName = "Chat")]
        public partial record ChatIdentification(long Value);

        [TypedEntityId]
        public partial record FolderIdentification(ProjectIdentification ProjectId, int FolderId);

        [TypedEntityId]
        public partial record ItemIdentification(FolderIdentification FolderId, int ItemId);

        [TypedStringValue(MinLength = 2, MaxLength = 60)]
        public sealed partial record SampleName(string Value);
        """;

    [Fact]
    public void GeneratedCode_HasNoDocumentationWarnings()
    {
        var parseOptions = new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose);
        var sourceTree = CSharpSyntaxTree.ParseText(Source, parseOptions);

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratedDocumentationTestAssembly",
            syntaxTrees: [sourceTree],
            references: GeneratorTestHelper.GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        CSharpGeneratorDriver
            .Create(
                generators: [new TypedEntityIdGenerator().AsSourceGenerator(), new TypedStringValueGenerator().AsSourceGenerator()],
                parseOptions: parseOptions)
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var generatedTrees = outputCompilation.SyntaxTrees.Where(tree => tree != sourceTree).ToList();
        generatedTrees.Count.ShouldBe(5);

        var diagnostics = outputCompilation.GetDiagnostics();
        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        // CS1591 — нет комментария; CS1572–CS1574 и CS1580/CS1584 — битые param/cref в комментариях
        var documentationWarnings = diagnostics
            .Where(d => d.Location.SourceTree is { } tree && generatedTrees.Contains(tree))
            .Where(d => d.Id.StartsWith("CS15", StringComparison.Ordinal))
            .Select(d => d.ToString())
            .ToList();

        documentationWarnings.ShouldBeEmpty();
    }
}
