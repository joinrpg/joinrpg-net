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

    public Task<ClaimInfo?> GetClaimInfoOrDefault(ClaimIdentification claimId) => throw new NotSupportedException();

    public Task<IReadOnlyDictionary<ClaimIdentification, ClaimInfo>> GetClaimInfos(IReadOnlyCollection<ClaimIdentification> claimIds)
        => throw new NotSupportedException();

    public Task<IReadOnlyDictionary<CharacterIdentification, ClaimInfo>> GetApprovedClaimInfos(IReadOnlyCollection<CharacterInfo> characters)
        => throw new NotSupportedException();
}
