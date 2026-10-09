using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Dal.Impl.Repositories.Characters;

/// <summary>
/// Загрузчик <see cref="ClaimInfo"/> (ADR021) поверх загрузчиков персонажа и профиля.
/// </summary>
/// <remarks>
/// Персонажи берутся одним <c>GetCharacterInfosByClaims</c>, профили — одним
/// <c>GetRequiredUserInfos</c>, сколько бы заявок ни пришло. Поштучная загрузка превратила бы
/// списки заявок в N+1.
/// </remarks>
internal class ClaimInfoRepository(
    ICharacterInfoRepository characterInfoRepository,
    IUserRepository userRepository) : IClaimInfoRepository
{
    public async Task<ClaimInfo?> GetClaimInfoOrDefault(ClaimIdentification claimId)
    {
        var character = await characterInfoRepository.GetCharacterInfoByClaimOrDefault(claimId);
        if (character is null)
        {
            return null;
        }

        var claim = new ClaimInCharacter(character, claimId);
        return new ClaimInfo(claim, await userRepository.GetRequiredUserInfo(claim.Claim.PlayerId));
    }

    public async Task<IReadOnlyDictionary<ClaimIdentification, ClaimInfo>> GetClaimInfos(
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
            .Distinct()
            .Select(claimId => new ClaimInCharacter(
                charactersByClaim.GetValueOrDefault(claimId)
                    ?? throw new InvalidOperationException($"Character for claim {claimId} is not loaded"),
                claimId))
            .ToArray();

        var players = await LoadPlayers(requested);

        return requested.ToDictionary(
            claim => claim.ClaimId,
            claim => new ClaimInfo(claim, players[claim.Claim.PlayerId]));
    }

    public async Task<IReadOnlyDictionary<CharacterIdentification, ClaimInfo>> GetApprovedClaimInfos(
        IReadOnlyCollection<CharacterInfo> characters)
    {
        ArgumentNullException.ThrowIfNull(characters);

        ClaimInCharacter[] approved = [..
            characters
                .DistinctBy(character => character.Id)
                .Where(character => character.ApprovedClaim is not null)
                .Select(character => new ClaimInCharacter(character, character.ApprovedClaim!))];

        var players = await LoadPlayers(approved);

        return approved.ToDictionary(
            claim => claim.Character.Id,
            claim => new ClaimInfo(claim, players[claim.Claim.PlayerId]));
    }

    /// <summary>Профили игроков заявок — одним запросом, а не по заявке.</summary>
    private async Task<Dictionary<UserIdentification, UserInfo>> LoadPlayers(IReadOnlyCollection<ClaimInCharacter> claims)
    {
        IReadOnlyCollection<UserIdentification> playerIds = [.. claims.Select(c => c.Claim.PlayerId).Distinct()];

        if (playerIds.Count == 0)
        {
            return [];
        }

        return (await userRepository.GetRequiredUserInfos(playerIds)).ToDictionary(user => user.UserId);
    }
}
