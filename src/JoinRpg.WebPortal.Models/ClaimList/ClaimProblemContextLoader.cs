using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Web.Models.ClaimList;

/// <summary>
/// Собирает <see cref="ClaimInfo"/> для списка заявок — двумя запросами на весь список,
/// а не по заявке.
/// </summary>
/// <remarks>
/// Проблемы заявки считаются по доменным сущностям (ADR013), а значит каждой заявке нужны персонаж
/// и профиль игрока. Поштучная загрузка превратила бы страницу списка в N+1, поэтому персонажи
/// берутся одним <c>GetCharacterInfosByClaims</c>, а профили — одним
/// <c>GetRequiredUserInfos</c>.
/// </remarks>
public static class ClaimProblemContextLoader
{
    public static async Task<IReadOnlyDictionary<ClaimIdentification, ClaimInfo>> Load(
        ICharacterInfoRepository characterInfoRepository,
        IUserRepository userRepository,
        IReadOnlyCollection<ClaimIdentification> claimIds)
    {
        ArgumentNullException.ThrowIfNull(claimIds);

        if (claimIds.Count == 0)
        {
            return new Dictionary<ClaimIdentification, ClaimInfo>();
        }

        var characters = await characterInfoRepository.GetCharacterInfosByClaims(claimIds);

        // Заявка в агрегате лежит у своего персонажа, а вход у нас — идентификаторы заявок,
        // поэтому разворачиваем связь один раз.
        var charactersByClaim = characters
            .SelectMany(character => character.Claims.Select(claim => (claim.ClaimId, character)))
            .ToDictionary(pair => pair.ClaimId, pair => pair.character);

        var requested = claimIds
            .Select(claimId => (
                ClaimId: claimId,
                Character: charactersByClaim.GetValueOrDefault(claimId)
                    ?? throw new InvalidOperationException(
                        $"Character for claim {claimId} is not loaded, cannot calculate problems")))
            .ToArray();

        IReadOnlyCollection<UserIdentification> playerIds =
            [.. requested.Select(r => r.Character.GetClaimById(r.ClaimId).PlayerId).Distinct()];

        var players = (await userRepository.GetRequiredUserInfos(playerIds)).ToDictionary(user => user.UserId);

        return requested.ToDictionary(
            r => r.ClaimId,
            r => BuildContext(r.Character, r.ClaimId, players));
    }

    /// <summary>
    /// Контекст одной заявки поверх уже загруженных персонажа и профилей игроков.
    /// </summary>
    private static ClaimInfo BuildContext(
        CharacterInfo character,
        ClaimIdentification claimId,
        Dictionary<UserIdentification, UserInfo> players)
    {
        var claim = new ClaimInCharacter(character, claimId);
        return new ClaimInfo(claim, players[claim.Claim.PlayerId]);
    }
}
