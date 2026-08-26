using JoinRpg.Common.PrimitiveTypes;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JoinRpg.Common.EntityFrameworkCore;

/// <summary>
/// Общий конвертер для любого id-типа, сгенерированного через [TypedEntityId] и реализующего
/// <see cref="IEntityId{TSelf, TValue}"/> — новый id-тип не требует отдельного класса-конвертера,
/// достаточно объявить у него ": IEntityId&lt;TSelf, TValue&gt;" (генератор делает это сам для не составных id).
/// </summary>
internal sealed class EntityIdValueConverter<TId, TValue>()
    : ValueConverter<TId, TValue>(id => id.Id, value => FromProvider(value))
    where TId : class, IEntityId<TId, TValue>
    where TValue : struct
{
    // Expression trees don't allow direct calls to static abstract interface members (CS8927),
    // so the call is routed through this regular method instead.
    private static TId FromProvider(TValue value) => TId.FromOptional(value);
}
