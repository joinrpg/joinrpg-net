namespace JoinRpg.DomainTypes.Characters.Claims;

/// <summary>
/// Персонаж с точки зрения правил заявки — ровно то, что этим правилам нужно знать.
/// </summary>
/// <remarks>
/// Шов на время миграции по ADR013: <see cref="CharacterInfo"/> реализует его нативно, а поверх
/// EF-сущности <c>Character</c> лежит обёртка в <c>JoinRpg.Domain</c>. Когда последний вызов
/// правил перестанет ходить через EF, обёртка и этот интерфейс станут не нужны — правила смогут
/// принимать <see cref="CharacterInfo"/> напрямую.
/// </remarks>
public interface IClaimTarget
{
    bool IsActive { get; }

    CharacterTypeInfo CharacterTypeInfo { get; }

    /// <summary>Утверждённая заявка, если персонаж занят.</summary>
    ClaimIdentification? ApprovedClaimId { get; }

    /// <summary>У этого игрока уже есть активная заявка на этого персонажа.</summary>
    bool HasActiveClaimOf(UserIdentification userId);
}
