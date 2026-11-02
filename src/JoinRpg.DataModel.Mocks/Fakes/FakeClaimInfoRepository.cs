using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.DataModel.Mocks.Fakes;

/// <summary>
/// Загрузчик заявок с персонажами поверх мока — общий для всех тестовых проектов.
/// </summary>
/// <remarks>
/// Реализовано то, что нужно проверяемым вызывающим; остальное бросает
/// <see cref="NotSupportedException"/>, чтобы поход за незапланированными данными был виден в тесте.
/// </remarks>
public sealed class FakeClaimInfoRepository(MockedProject mock) : IClaimInfoRepository
{
    public async Task<ClaimInCharacter?> GetClaimInCharacterOrDefault(ClaimIdentification claimId)
        => await ((ICharacterInfoRepository)new FakeCharacterInfoRepository(mock)).GetCharacterInfoByClaimOrDefault(claimId) is { } character
            ? new ClaimInCharacter(character, claimId)
            : null;

    /// <summary>
    /// Профиль у мока один — <c>mock.Player</c>; заявки от других игроков «не загружаются»,
    /// как и остальные незапланированные походы за данными.
    /// </summary>
    public async Task<ClaimInfo?> GetClaimInfoOrDefault(ClaimIdentification claimId)
    {
        // Имитация relationship fixup EF6: заявке, созданной сервисом в рамках теста, навигации
        // ещё не проставлены — как ReInitProjectInfo делает это для ACL и типов оплат.
        foreach (var claim in mock.Project.Claims)
        {
            claim.Player ??= claim.PlayerUserId == mock.Player.UserId
                ? mock.Player
                : throw new NotSupportedException();
            claim.FinanceOperations ??= [];
        }

        var claimInCharacter = await GetClaimInCharacterOrDefault(claimId);
        if (claimInCharacter is null)
        {
            return null;
        }

        return claimInCharacter.Claim.PlayerId == mock.Player.UserId
            ? new ClaimInfo(claimInCharacter, mock.PlayerInfo)
            : throw new NotSupportedException();
    }

    /// <summary>Пачкой поверх поштучной загрузки; отсутствующая заявка падает, как у настоящего.</summary>
    public async Task<IReadOnlyDictionary<ClaimIdentification, ClaimInfo>> GetClaimInfos(IReadOnlyCollection<ClaimIdentification> claimIds)
    {
        var result = new Dictionary<ClaimIdentification, ClaimInfo>();
        foreach (var claimId in claimIds.Distinct())
        {
            result.Add(claimId, await GetClaimInfoOrDefault(claimId)
                ?? throw new NotSupportedException($"Заявка {claimId} не создана моком"));
        }
        return result;
    }

    public Task<IReadOnlyDictionary<CharacterIdentification, ClaimInfo>> GetApprovedClaimInfos(IReadOnlyCollection<CharacterInfo> characters)
        => throw new NotSupportedException();
}
