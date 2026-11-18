using System.Diagnostics.CodeAnalysis;

namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>
/// Реализуется типами, сгенерированными через <see cref="TypedEntityIdAttribute"/> для не составных id
/// (единственный числовой параметр). Используется для универсального маппинга в EF Core через
/// generic ValueConverter в JoinRpg.Common.EntityFrameworkCore.
/// </summary>
public interface IEntityId<TSelf, TValue>
    where TSelf : class, IEntityId<TSelf, TValue>
    where TValue : struct
{
    TValue Id { get; }

    [return: NotNullIfNotNull(nameof(value))]
    static abstract TSelf? FromOptional(TValue? value);
}
