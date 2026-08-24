namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>Маркер для генератора реализаций типизированных строковых значений.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class TypedStringValueAttribute : Attribute
{
    /// <summary>Минимальная длина строки (по умолчанию 1).</summary>
    public int MinLength { get; set; } = 1;

    /// <summary>Максимальная длина строки (по умолчанию 999).</summary>
    public int MaxLength { get; set; } = 999;

    /// <summary>Нужно ли обрезать пробелы по краям (по умолчанию true).</summary>
    public bool Trim { get; set; } = true;
}
