namespace JoinRpg.DomainTypes.Characters;

/// <summary>
/// Минимум сведений о персонаже, по которым решается, доступно ли ему поле проекта
/// (см. <c>FieldExtensions.IsAvailableForTarget</c>) — тип персонажа и его группы вверх до корня.
/// </summary>
/// <remarks>
/// Существует ради того, чтобы правило доступности поля было записано ровно один раз и работало
/// и над доменным агрегатом <see cref="CharacterInfo"/> (ADR013), и над EF-сущностью, пока та ещё
/// не мигрирована.
/// </remarks>
public interface IFieldAvailabilityTarget
{
    CharacterType CharacterType { get; }

    /// <summary>Все группы персонажа вверх до корня, включая прямые.</summary>
    IReadOnlyCollection<CharacterGroupIdentification> ParentGroupIdsToTop { get; }
}
