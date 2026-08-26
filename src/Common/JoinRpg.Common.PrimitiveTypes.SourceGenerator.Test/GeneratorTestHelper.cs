using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace JoinRpg.Common.PrimitiveTypes.SourceGenerator.Test;

/// <summary>
/// Прогоняет <see cref="TypedEntityIdGenerator"/> над куском исходного кода и компилирует результат,
/// чтобы можно было не только проверить диагностику, но и вызвать сгенерированный код через рефлексию.
/// </summary>
internal static class GeneratorTestHelper
{
    public static GeneratorRunResult RunGenerator(string source)
    {
        var references = GetMetadataReferences();

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTestAssembly",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new TypedEntityIdGenerator());
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

        var compileDiagnostics = outputCompilation.GetDiagnostics();

        return new GeneratorRunResult(outputCompilation, generatorDiagnostics, compileDiagnostics);
    }

    /// <summary>Компилирует результат генерации в assembly и загружает его для рефлексии.</summary>
    public static Assembly EmitAndLoad(Compilation compilation)
    {
        using var stream = new MemoryStream();
        EmitResult result = compilation.Emit(stream);
        if (!result.Success)
        {
            var errors = string.Join(
                Environment.NewLine,
                result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            throw new InvalidOperationException($"Компиляция не удалась:{Environment.NewLine}{errors}");
        }

        stream.Seek(0, SeekOrigin.Begin);
        return Assembly.Load(stream.ToArray());
    }

    private static List<MetadataReference> GetMetadataReferences()
    {
        var trustedPlatformAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        var references = trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();

        references.Add(MetadataReference.CreateFromFile(typeof(TypedEntityIdAttribute).Assembly.Location));

        return references;
    }
}

internal sealed record GeneratorRunResult(
    Compilation OutputCompilation,
    IReadOnlyList<Diagnostic> GeneratorDiagnostics,
    IReadOnlyList<Diagnostic> CompileDiagnostics)
{
    public IEnumerable<Diagnostic> Errors =>
        GeneratorDiagnostics.Concat(CompileDiagnostics).Where(d => d.Severity == DiagnosticSeverity.Error);
}
