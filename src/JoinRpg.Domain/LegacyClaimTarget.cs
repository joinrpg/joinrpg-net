using JoinRpg.DataModel.Extensions;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain;

/// <summary>
/// EF-персонаж в роли цели заявки — мост на время миграции по ADR013.
/// </summary>
/// <remarks>
/// Нужен, пока часть вызывающего кода (view-модели персонажа и дерева ролей) не умеет работать с
/// <see cref="CharacterInfo"/>. Удалить вместе с последним таким вызовом — тогда правила будут
/// принимать агрегат напрямую.
/// </remarks>
[Obsolete("ADR013: удалить вместе с последним вызовом правил заявки поверх EF Character")]
internal sealed class LegacyClaimTarget(Character character) : IClaimTarget
{
    public bool IsActive => character.IsActive;

    // Маппинг флагов не дублируем: ToCharacterTypeInfo — единственное место, где он живёт для
    // EF-сущности, и он же добавляет к ошибке CharacterId, если флаги в БД противоречивы.
    public CharacterTypeInfo CharacterTypeInfo => character.ToCharacterTypeInfo();

    public ClaimIdentification? ApprovedClaimId => character.GetApprovedClaimIdOrDefault();

    public bool HasActiveClaimOf(UserIdentification userId)
        => character.Claims.OfUserActive(userId.Value).Any();
}
