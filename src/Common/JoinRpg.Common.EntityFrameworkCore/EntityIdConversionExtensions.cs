using JoinRpg.Common.PrimitiveTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoinRpg.Common.EntityFrameworkCore;

public static class EntityIdConversionExtensions
{
    /// <summary>
    /// Регистрирует <see cref="EntityIdValueConverter{TId, TValue}"/> для id-типа. Вызывается из
    /// ConfigureConventions (обязательно ДО построения модели — иначе EF примет свойство этого типа
    /// за навигацию к новой entity). TValue не выводится из TId одного generic-параметра, поэтому
    /// указывается явно: .HaveEntityIdValueConversion&lt;BastiliaProjectId, int&gt;().
    /// </summary>
    public static void HaveEntityIdValueConversion<TId, TValue>(this PropertiesConfigurationBuilder<TId> builder)
        where TId : class, IEntityId<TId, TValue>
        where TValue : struct
        => builder.HaveConversion<EntityIdValueConverter<TId, TValue>>();

    /// <summary>
    /// Помечает все однопроцессные PK-свойства, чей CLR-тип реализует <see cref="IEntityId{TSelf, TValue}"/>,
    /// как identity (ValueGenerated.OnAdd). Вызывается из OnModelCreating, когда модель уже построена.
    /// </summary>
    public static void EntityIdsSetValueGeneratedOnAdd(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.FindPrimaryKey() is { Properties: [var keyProperty] }
                && ImplementsEntityId(keyProperty.ClrType))
            {
                keyProperty.ValueGenerated = ValueGenerated.OnAdd;
            }
        }
    }

    // IEntityId<TSelf, TValue> — generic-интерфейс, поэтому "typeof(IEntityId<,>).IsAssignableFrom(clrType)"
    // не работает напрямую; ищем среди реализованных интерфейсов совпадение по открытой generic-форме.
    private static bool ImplementsEntityId(Type type) =>
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityId<,>));
}
