namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>Маркер для генератора реализаций типизированных идентификаторов.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class TypedEntityIdAttribute : Attribute
{
    /// <summary>Короткий префикс для ToString (например "ClaimId"). По умолчанию — TypeName минус суффикс "Identification" плюс суффикс "Id".</summary>
    public string? ShortName { get; set; }

    /// <summary>Дополнительные префиксы для парсинга (например, старые форматы). По умолчанию ShortName без суффикса "Id" добавляется автоматически.</summary>
    public string[]? AdditionalPrefixes { get; set; }
}
