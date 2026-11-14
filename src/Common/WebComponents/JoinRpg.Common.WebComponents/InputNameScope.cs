using Microsoft.AspNetCore.Components.Forms;

namespace JoinRpg.Common.WebComponents;

/// <summary>
/// Область имён полей формы: каскадируется формой (<see cref="JoinEditForm"/>), поля ввода
/// квалифицируют своё имя префиксом, чтобы браузерное автодополнение не принимало служебные
/// поля (название метода оплаты, расхода и т.п.) за персональные данные. Явный параметр <c>Name</c>
/// у поля побеждает над областью.
/// </summary>
internal sealed class InputNameScope
{
    public string Prefix { get; }

    public InputNameScope(string prefix) => Prefix = prefix;

    public string Qualify(string fieldName) => $"{Prefix}.{fieldName}";

    /// <summary>
    /// Эффективное имя поля ввода: явное <paramref name="explicitName"/> побеждает; иначе имя свойства
    /// из <paramref name="fieldIdentifier"/> квалифицируется областью (без области — как есть).
    /// </summary>
    public static string? Resolve(string? explicitName, InputNameScope? scope, in FieldIdentifier fieldIdentifier)
        => explicitName
            ?? (fieldIdentifier.FieldName is { } fieldName && scope is { } s
                ? s.Qualify(fieldName)
                : fieldIdentifier.FieldName);
}
