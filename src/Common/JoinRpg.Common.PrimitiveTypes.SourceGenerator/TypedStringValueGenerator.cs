using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JoinRpg.Common.PrimitiveTypes.SourceGenerator;

[Generator]
public class TypedStringValueGenerator : IIncrementalGenerator
{
    private const string AttributeFullName = "JoinRpg.Common.PrimitiveTypes.TypedStringValueAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var records = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeFullName,
                predicate: static (node, _) =>
                    node is RecordDeclarationSyntax r && r.ParameterList != null,
                transform: static (ctx, _) => GetTypeInfo(ctx))
            .Where(static t => t is not null)
            .Select(static (t, _) => t!);

        context.RegisterSourceOutput(records, static (ctx, info) =>
            ctx.AddSource($"{info.FullTypeName}.TypedStringValue.g.cs", GenerateCode(info)));
    }

    private static TypeGenerationInfo? GetTypeInfo(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetNode is not RecordDeclarationSyntax recordDecl ||
            ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        var attrData = ctx.Attributes.FirstOrDefault();
        if (attrData == null)
        {
            return null;
        }

        // Извлекаем свойства атрибута
        int minLength = 1;
        int maxLength = 999;
        bool trim = true;

        foreach (var namedArg in attrData.NamedArguments)
        {
            if (namedArg.Key == "MinLength" && namedArg.Value.Value is int ml)
            {
                minLength = ml;
            }
            else if (namedArg.Key == "MaxLength" && namedArg.Value.Value is int mx)
            {
                maxLength = mx;
            }
            else if (namedArg.Key == "Trim" && namedArg.Value.Value is bool t)
            {
                trim = t;
            }
        }

        // Проверяем, что record имеет один параметр типа string
        if (recordDecl.ParameterList == null)
        {
            return null;
        }

        var paramList = recordDecl.ParameterList.Parameters;
        if (paramList.Count != 1)
        {
            return null;
        }

        var param = paramList[0];
        if (param.Type == null)
        {
            return null;
        }

        var typeInfo = ctx.SemanticModel.GetTypeInfo(param.Type);
        if (typeInfo.Type?.SpecialType != SpecialType.System_String)
        {
            return null;
        }

        // Определяем пространство имён
        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : typeSymbol.ContainingNamespace.ToDisplayString();

        // Проверяем наличие уже реализованных интерфейсов
        bool hasIEquatableString = typeSymbol.AllInterfaces
            .Any(i => i.IsGenericType &&
                     i.OriginalDefinition.ToString() == "System.IEquatable<T>" &&
                     i.TypeArguments.Length == 1 &&
                     i.TypeArguments[0].SpecialType == SpecialType.System_String);

        bool hasISpanParsable = typeSymbol.AllInterfaces
            .Any(i => i.IsGenericType &&
                     i.OriginalDefinition.ToString().Contains("ISpanParsable") &&
                     i.TypeArguments.Length == 1 &&
                     i.TypeArguments[0].Equals(typeSymbol, SymbolEqualityComparer.Default));

        bool hasIComparable = typeSymbol.AllInterfaces
            .Any(i => i.IsGenericType &&
                     i.OriginalDefinition.ToString() == "System.IComparable<T>" &&
                     i.TypeArguments.Length == 1 &&
                     i.TypeArguments[0].Equals(typeSymbol, SymbolEqualityComparer.Default));

        // Проверяем наличие пользовательских методов
        bool hasCustomValidateMethod = typeSymbol.GetMembers("CustomValidateAndCanonicalize")
            .Any(m => m is IMethodSymbol method &&
                     method.IsStatic &&
                     method.ReturnType.SpecialType == SpecialType.System_String &&
                     method.Parameters.Length == 1 &&
                     method.Parameters[0].Type.SpecialType == SpecialType.System_String);

        bool hasExplicitToString = typeSymbol.GetMembers("ToString")
            .Any(m => m is IMethodSymbol method && method.IsOverride && method.Parameters.Length == 0 && method.DeclaringSyntaxReferences.Length > 0);
        bool hasToStringMethod = hasExplicitToString && !typeSymbol.IsRecord;

        bool hasFromOptionalMethod = typeSymbol.GetMembers("FromOptional")
            .Any(m => m is IMethodSymbol method &&
                     method.IsStatic &&
                     method.ReturnType is INamedTypeSymbol returnType && returnType.IsGenericType &&
                     returnType.OriginalDefinition.ToString() == "System.Nullable<T>" &&
                     returnType.TypeArguments.Length == 1 &&
                     returnType.TypeArguments[0].Equals(typeSymbol, SymbolEqualityComparer.Default) &&
                     method.Parameters.Length == 1 &&
                     method.Parameters[0].Type.SpecialType == SpecialType.System_String);

        bool hasImplicitOperatorFromString = typeSymbol.GetMembers("op_Implicit")
            .Any(m => m is IMethodSymbol method &&
                     method.IsStatic &&
                     method.ReturnType.Equals(typeSymbol, SymbolEqualityComparer.Default) &&
                     method.Parameters.Length == 1 &&
                     method.Parameters[0].Type.SpecialType == SpecialType.System_String);

        bool hasImplicitOperatorToString = typeSymbol.GetMembers("op_Implicit")
            .Any(m => m is IMethodSymbol method &&
                     method.IsStatic &&
                     method.ReturnType.SpecialType == SpecialType.System_String &&
                     method.Parameters.Length == 1 &&
                     method.Parameters[0].Type.Equals(typeSymbol, SymbolEqualityComparer.Default));

        bool hasParseMethods = typeSymbol.GetMembers("Parse")
            .Any(m => m is IMethodSymbol method && method.IsStatic) ||
            typeSymbol.GetMembers("TryParse")
            .Any(m => m is IMethodSymbol method && method.IsStatic);

        return new TypeGenerationInfo(
            FullTypeName: $"{(ns != null ? ns + "." : "")}{typeSymbol.Name}",
            Namespace: ns,
            TypeName: typeSymbol.Name,
            MinLength: minLength,
            MaxLength: maxLength,
            Trim: trim,
            HasIEquatableString: hasIEquatableString,
            HasISpanParsable: hasISpanParsable,
            HasCustomValidateMethod: hasCustomValidateMethod,
            HasToStringMethod: hasToStringMethod,
            HasFromOptionalMethod: hasFromOptionalMethod,
            HasImplicitOperatorFromString: hasImplicitOperatorFromString,
            HasImplicitOperatorToString: hasImplicitOperatorToString,
            HasParseMethods: hasParseMethods,
            HasIComparable: hasIComparable
        );
    }

    private static string GenerateCode(TypeGenerationInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Diagnostics.CodeAnalysis;");
        sb.AppendLine("using JoinRpg.Common.PrimitiveTypes;");
        sb.AppendLine();

        if (info.Namespace != null)
        {
            sb.AppendLine($"namespace {info.Namespace};");
            sb.AppendLine();
        }

        var interfaces = new List<string>();
        if (!info.HasIEquatableString)
        {
            interfaces.Add("IEquatable<string>");
        }

        if (!info.HasISpanParsable && !info.HasParseMethods)
        {
            interfaces.Add($"ISpanParsable<{info.TypeName}>");
        }

        if (!info.HasIComparable)
        {
            interfaces.Add($"IComparable<{info.TypeName}>");
        }

        var interfaceString = interfaces.Count > 0 ? " : " + string.Join(", ", interfaces) : "";
        sb.AppendLine($"public partial record {info.TypeName}{interfaceString}");
        sb.AppendLine("{");



        // Статический метод ValidateAndCanonicalize
        sb.AppendLine($"    private static string ValidateAndCanonicalize(string value)");
        sb.AppendLine("    {");
        if (info.Trim)
        {
            sb.AppendLine("        value = value.Trim();");
        }
        sb.AppendLine($"        if (value.Length < {info.MinLength})");
        sb.AppendLine("        {");
        sb.AppendLine($"            throw new ArgumentException($\"Значение должно быть не короче {info.MinLength} символов\", nameof(value));");
        sb.AppendLine("        }");
        sb.AppendLine($"        if (value.Length > {info.MaxLength})");
        sb.AppendLine("        {");
        sb.AppendLine($"            throw new ArgumentException($\"Значение должно быть не длиннее {info.MaxLength} символов\", nameof(value));");
        sb.AppendLine("        }");
        if (info.HasCustomValidateMethod)
        {
            sb.AppendLine("        // Вызов пользовательского метода");
            sb.AppendLine("        value = CustomValidateAndCanonicalize(value);");
        }
        sb.AppendLine("        return value;");
        sb.AppendLine("    }");
        sb.AppendLine();


        sb.AppendLine();

        // Неявное преобразование из string
        if (!info.HasImplicitOperatorFromString)
        {
            sb.AppendLine($"    public static implicit operator {info.TypeName}(string value) => new {info.TypeName}(ValidateAndCanonicalize(value));");
        }
        sb.AppendLine();

        // Неявное преобразование в string
        if (!info.HasImplicitOperatorToString)
        {
            sb.AppendLine($"    public static implicit operator string({info.TypeName} self) => self.Value;");
        }
        sb.AppendLine();

        // IEquatable<string>
        if (!info.HasIEquatableString)
        {
            sb.AppendLine($"    public bool Equals(string? other) => Value == other;");
            sb.AppendLine();
        }

        // IComparable<T>
        if (!info.HasIComparable)
        {
            sb.AppendLine($"    public int CompareTo({info.TypeName}? other) => Value.CompareTo(other?.Value);");
            sb.AppendLine();
        }

        // FromOptional
        if (!info.HasFromOptionalMethod)
        {
            sb.AppendLine($"    public static {info.TypeName}? FromOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : new {info.TypeName}(ValidateAndCanonicalize(value));");
        }
        sb.AppendLine();

        // ToString
        if (!info.HasToStringMethod)
        {
            sb.AppendLine($"    public override string ToString() => $\"{info.TypeName}({{Value}})\";");
        }
        sb.AppendLine();

        // ISpanParsable реализация
        if (!info.HasISpanParsable && !info.HasParseMethods)
        {
            GenerateISpanParsable(sb, info);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void GenerateISpanParsable(StringBuilder sb, TypeGenerationInfo info)
    {
        sb.AppendLine($"    public static {info.TypeName} Parse(ReadOnlySpan<char> value, IFormatProvider? provider)");
        sb.AppendLine($"        => TryParse(value, provider, out var result) ? result : throw new ArgumentException(\"Could not parse supplied value.\", nameof(value));");
        sb.AppendLine();
        sb.AppendLine($"    public static {info.TypeName} Parse(string value, IFormatProvider? provider = null)");
        sb.AppendLine($"        => TryParse(value.AsSpan(), provider, out var result) ? result : throw new ArgumentException(\"Could not parse supplied value.\", nameof(value));");
        sb.AppendLine();
        sb.AppendLine($"    static bool IParsable<{info.TypeName}>.TryParse([NotNullWhen(true)] string? value, IFormatProvider? provider, [MaybeNullWhen(false)] out {info.TypeName} result)");
        sb.AppendLine($"        => TryParse(value.AsSpan(), provider, out result);");
        sb.AppendLine();
        sb.AppendLine($"    public static bool TryParse(string? value, IFormatProvider? provider, [MaybeNullWhen(false)] out {info.TypeName} result)");
        sb.AppendLine($"        => TryParse(value.AsSpan(), provider, out result);");
        sb.AppendLine();
        sb.AppendLine($"    public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? provider, [MaybeNullWhen(false)] out {info.TypeName} result)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Удаляем префикс типа, если есть");
        sb.AppendLine($"        var trimmed = IdentificationParseHelper.RemovePrefixes(value, [nameof({info.TypeName}), \"{info.TypeName}\"]);");
        sb.AppendLine("        // Если после удаления префикса осталась непустая строка, пытаемся создать значение");
        sb.AppendLine("        if (!trimmed.IsEmpty)");
        sb.AppendLine("        {");
        sb.AppendLine("            try");
        sb.AppendLine("            {");
        sb.AppendLine($"                result = new {info.TypeName}(ValidateAndCanonicalize(trimmed.ToString()));");
        sb.AppendLine("                return true;");
        sb.AppendLine("            }");
        sb.AppendLine("            catch (ArgumentException)");
        sb.AppendLine("            {");
        sb.AppendLine("                // Значение не прошло валидацию");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        result = null;");
        sb.AppendLine("        return false;");
        sb.AppendLine("    }");
    }

    private sealed record TypeGenerationInfo(
        string FullTypeName,
        string? Namespace,
        string TypeName,
        int MinLength,
        int MaxLength,
        bool Trim,
        bool HasIEquatableString,
        bool HasISpanParsable,
        bool HasCustomValidateMethod,
        bool HasToStringMethod,
        bool HasFromOptionalMethod,
        bool HasImplicitOperatorFromString,
        bool HasImplicitOperatorToString,
        bool HasParseMethods,
        bool HasIComparable
    );
}
