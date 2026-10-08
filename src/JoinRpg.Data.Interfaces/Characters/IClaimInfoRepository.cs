using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Data.Interfaces.Characters;

/// <summary>
/// Загрузка <see cref="ClaimInfo"/> — заявки вместе с её персонажем и профилем игрока (ADR021).
/// </summary>
/// <remarks>
/// Своего SQL здесь нет: персонажи берутся из <see cref="ICharacterInfoRepository"/>, профили —
/// из <see cref="IUserRepository"/>. Отдельный интерфейс, а не расширения над этой парой, затем,
/// чтобы вызывающему хватало одной зависимости, а в тестах загрузку подделывал один фейк.
/// </remarks>
public interface IClaimInfoRepository
{
    /// <summary>Заявка вместе с персонажем и игроком. <c>null</c>, если такой заявки нет.</summary>
    Task<ClaimInfo?> GetClaimInfoOrDefault(ClaimIdentification claimId);

    /// <summary>
    /// Заявка вместе с персонажем, без профиля игрока. <c>null</c>, если такой заявки нет.
    /// </summary>
    /// <remarks>
    /// Для тех, кому профиль не нужен (например, виджет поселения, ADR022): на один запрос меньше,
    /// чем у <see cref="GetClaimInfoOrDefault"/>.
    /// </remarks>
    Task<ClaimInCharacter?> GetClaimInCharacterOrDefault(ClaimIdentification claimId);

    /// <summary>
    /// Заявки вместе с персонажами и игроками — двумя запросами на весь список, а не по заявке.
    /// Все заявки должны быть из одного проекта.
    /// </summary>
    /// <exception cref="InvalidOperationException">Какой-то из заявок нет.</exception>
    /// <exception cref="ArgumentException">Заявки из разных проектов.</exception>
    Task<IReadOnlyDictionary<ClaimIdentification, ClaimInfo>> GetClaimInfos(IReadOnlyCollection<ClaimIdentification> claimIds);

    /// <summary>
    /// Утверждённые заявки уже загруженных персонажей вместе с игроками — одним запросом профилей
    /// на весь список.
    /// </summary>
    /// <remarks>
    /// Персонаж без утверждённой заявки — нормальный случай, и в словарь он просто не попадает:
    /// <see cref="ClaimInfo"/> с пустым игроком не бывает. Персонажи не перечитываются, поэтому
    /// <see cref="ClaimInfo.Character"/> — тот же экземпляр, что пришёл на вход.
    /// </remarks>
    Task<IReadOnlyDictionary<CharacterIdentification, ClaimInfo>> GetApprovedClaimInfos(IReadOnlyCollection<CharacterInfo> characters);
}
